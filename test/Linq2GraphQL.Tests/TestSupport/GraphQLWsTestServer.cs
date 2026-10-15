using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Linq2GraphQL.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.Tests;

/// <summary>
///     One accepted WebSocket from the server side: lets a test push protocol messages,
///     close with a chosen code, or abort the TCP connection (transient drop).
/// </summary>
internal sealed class WsTestSession(WebSocket socket)
{
    public Task SendText(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public Task SendNext(string id, string payloadJson)
    {
        return SendText($$"""{"type":"next","id":"{{id}}","payload":{{payloadJson}}}""");
    }

    public Task SendError(string id, string payloadJson)
    {
        return SendText($$"""{"type":"error","id":"{{id}}","payload":{{payloadJson}}}""");
    }

    public Task SendComplete(string id)
    {
        return SendText($$"""{"type":"complete","id":"{{id}}"}""");
    }

    public Task SendPing()
    {
        return SendText("""{"type":"ping"}""");
    }

    public Task SendEmptyFrame()
    {
        return socket.SendAsync(Array.Empty<byte>(), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public Task Close(int closeCode, string description = "")
    {
        return socket.CloseAsync((WebSocketCloseStatus)closeCode, description, CancellationToken.None);
    }

    /// <summary>Drops the TCP connection without a close frame (recoverable-disconnect test).</summary>
    public void Abort()
    {
        socket.Abort();
    }
}

/// <summary>
///     A real in-process GraphQL-over-WebSocket endpoint (Kestrel on a random loopback
///     port). Default behavior: acks connection_init, records incoming messages, and
///     hands each accepted socket to the test as a WsTestSession. Tests script the
///     server side by awaiting the received-message signals and calling session methods.
/// </summary>
internal sealed class GraphQLWsTestServer : IAsyncDisposable
{
    private WebApplication? app;
    private readonly SemaphoreSlim sessionsGate = new(1, 1);

    /// <summary>Subprotocol to accept; empty accepts none (subprotocol-violation tests).</summary>
    public string SubProtocol { get; set; } = "graphql-transport-ws";

    /// <summary>Delay before the automatic connection_ack is sent (default: immediately).</summary>
    public TimeSpan AckDelay { get; set; } = TimeSpan.Zero;

    /// <summary>Never send connection_ack (drives the client's 10 s ack timeout).</summary>
    public bool NeverAck { get; set; }

    /// <summary>Reject this many upcoming upgrade requests with HTTP 503 (reconnect-retry tests).</summary>
    public int RejectUpgrades;

    /// <summary>Number of upgrade requests rejected so far.</summary>
    public int RejectedUpgrades;

    public ConcurrentQueue<string> Incoming { get; } = new();
    public List<WsTestSession> Sessions { get; } = new();

    /// <summary>Set when a connection_init arrives; payload is the raw message text.</summary>
    public TaskCompletionSource<string> InitReceived { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Set when a subscribe/start arrives; payload is the raw message text.</summary>
    public TaskCompletionSource<string> SubscribeReceived { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Test hook: invoked for every incoming message after the built-in routing.</summary>
    public Func<WsTestSession, string, Task>? OnMessage { get; set; }

    public int Port { get; private set; }

    public string HttpBaseAddress => $"http://127.0.0.1:{Port}/graphql/";

    public static async Task<GraphQLWsTestServer> StartAsync()
    {
        var server = new GraphQLWsTestServer();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting("urls", "http://127.0.0.1:0");
        server.app = builder.Build();

        server.app.UseWebSockets();
        server.app.Map("/graphql", server.HandleAsync);
        await server.app.StartAsync();

        // UseUrls("...:0") resolves to the actual port only after start.
        var address = server.app.Urls.First(u => u.StartsWith("http://127.0.0.1"));
        server.Port = int.Parse(address.Split(':').Last());
        return server;
    }

    private async Task HandleAsync(HttpContext context)
    {
        if (Interlocked.Decrement(ref RejectUpgrades) >= 0)
        {
            Interlocked.Increment(ref RejectedUpgrades);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        Interlocked.Exchange(ref RejectUpgrades, 0);
        var subProtocol = string.IsNullOrEmpty(SubProtocol) ? null : SubProtocol;
        using var socket = await context.WebSockets.AcceptWebSocketAsync(subProtocol);
        var session = new WsTestSession(socket);
        await sessionsGate.WaitAsync();
        try
        {
            Sessions.Add(session);
        }
        finally
        {
            sessionsGate.Release();
        }

        await RunSessionAsync(session, socket);
    }

    private async Task RunSessionAsync(WsTestSession session, WebSocket socket)
    {
        var buffer = new byte[64 * 1024];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Incoming.Enqueue(text);
                await RouteAsync(session, text);
            }
        }
        catch (WebSocketException)
        {
            // Client aborted the connection (transient-drop test) — normal end.
        }
    }

    private async Task RouteAsync(WsTestSession session, string text)
    {
        if (text.Contains("connection_init"))
        {
            InitReceived.TrySetResult(text);
            if (!NeverAck)
            {
                if (AckDelay > TimeSpan.Zero)
                {
                    await Task.Delay(AckDelay);
                }

                await session.SendText("""{"type":"connection_ack"}""");
            }
        }
        else if (text.Contains("\"subscribe\"") || text.Contains("\"start\""))
        {
            SubscribeReceived.TrySetResult(text);
        }

        if (OnMessage is not null)
        {
            await OnMessage(session, text);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}

internal static class WsTestGraphClientFactory
{
    /// <summary>A GraphClient (safe mode OFF) whose SubscriptionUrl points at the test server.</summary>
    public static GraphClient Create(GraphQLWsTestServer server,
        SubscriptionProtocol protocol = SubscriptionProtocol.GraphQLWebSocket)
    {
        var options = new GraphClientOptions
        {
            SubscriptionProtocol = protocol,
            UseSafeMode = false,
        };
        var httpClient = new HttpClient { BaseAddress = new Uri(server.HttpBaseAddress) };
        return new GraphClient(httpClient, Options.Create(options), new ServiceCollection().BuildServiceProvider());
    }
}
