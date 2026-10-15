using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using Linq2GraphQL.Client;
using System.Reactive.Linq;
using Linq2GraphQL.Client.Subscriptions;

namespace Linq2GraphQL.Tests;

/// <summary>
///     End-to-end tests of WSClient against a real in-process WebSocket endpoint
///     (Kestrel on a random loopback port). Covers the message switch (next/error/
///     complete/ping/unknown), handshake failures, fatal close codes, and the
///     transient-disconnect reconnect cycle.
/// </summary>
public class WsClientTests
{
    private static (WSClient client, List<JsonElement> payloads, List<GraphQueryExecutionException> errors,
        List<Exception> failures) CreateClient(GraphClient graphClient)
    {
        var request = new GraphQLRequest { Query = "subscription { customerAdded { customerId } }" };
        var client = new WSClient(graphClient, request);
        var payloads = new List<JsonElement>();
        var errors = new List<GraphQueryExecutionException>();
        var failures = new List<Exception>();
        client.Payloads.Subscribe(payloads.Add);
        client.Errors.Subscribe(errors.Add);
        client.Failures.Subscribe(failures.Add);
        return (client, payloads, errors, failures);
    }

    private static string GetSubscriptionId(string subscribeMessage)
    {
        using var document = JsonDocument.Parse(subscribeMessage);
        return document.RootElement.GetProperty("id").GetString()
               ?? throw new InvalidDataException("Subscribe message had no id.");
    }

    private static async Task WaitForIncomingAsync(GraphQLWsTestServer server, Func<string, bool> predicate,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (server.Incoming.Any(predicate))
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.Fail("Expected incoming message was not observed before the timeout.");
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int seconds = 10)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.Fail("Condition was not met before the timeout.");
    }

    [Fact]
    public async Task UnknownMessageType_IsIgnored_SubscriptionContinues()
    {
        await using var server = await GraphQLWsTestServer.StartAsync();
        var graphClient = WsTestGraphClientFactory.Create(server);
        var (client, payloads, errors, failures) = CreateClient(graphClient);

        // After the client subscribes: an unknown message type first (must be ignored),
        // then a real next — the subscription must survive and deliver.
        server.OnMessage = async (session, text) =>
        {
            if (text.Contains("connection_init"))
            {
                return;
            }

            if (text.Contains("subscribe"))
            {
                var id = GetSubscriptionId(text);
                await session.SendText("""{"type":"mystery","id":"someone-else"}""");
                await session.SendNext(id, """{"customerId":"11111111-1111-1111-1111-111111111111"}""");
            }
        };

        await using var _ = client;
        await client.Start().WaitAsync(TimeSpan.FromSeconds(10));
        // Wait on what the CLIENT delivered, not on what the server received: the server-side
        // send is not the same moment as the client processing it.
        await WaitUntilAsync(() => payloads.Count >= 1);

        Assert.Single(payloads);
        Assert.Equal("11111111-1111-1111-1111-111111111111",
            payloads[0].GetProperty("customerId").GetString());
        Assert.Empty(errors);
        Assert.Empty(failures);
    }

    [Fact]
    public async Task NextForOtherSubscriptionId_IsIgnored()
    {
        await using var server = await GraphQLWsTestServer.StartAsync();
        var graphClient = WsTestGraphClientFactory.Create(server);
        var (client, payloads, errors, failures) = CreateClient(graphClient);

        server.OnMessage = async (session, text) =>
        {
            if (text.Contains("subscribe"))
            {
                var id = GetSubscriptionId(text);
                await session.SendNext("other-subscription", """{"customerId":"22222222-2222-2222-2222-222222222222"}""");
                await session.SendNext(id, """{"customerId":"33333333-3333-3333-3333-333333333333"}""");
            }
        };

        await using var _ = client;
        await client.Start().WaitAsync(TimeSpan.FromSeconds(10));
        // Ordering on one socket is preserved: the foreign event was sent before the matching one,
        // so once the matching payload is observed the foreign one has already been processed.
        await WaitUntilAsync(() => payloads.Count >= 1);

        var payload = Assert.Single(payloads);
        Assert.Equal("33333333-3333-3333-3333-333333333333", payload.GetProperty("customerId").GetString());
        Assert.Empty(failures);
    }

    [Fact]
    public async Task ErrorFromServer_SurfacesViaOnError()
    {
        await using var server = await GraphQLWsTestServer.StartAsync();
        var graphClient = WsTestGraphClientFactory.Create(server);
        var (client, payloads, errors, failures) = CreateClient(graphClient);

        server.OnMessage = async (session, text) =>
        {
            if (text.Contains("subscribe"))
            {
                var id = GetSubscriptionId(text);
                // A live sentinel event first: proves the stream delivers, so the no-payload check
                // below is meaningful instead of passing because nothing was delivered at all.
                await session.SendNext(id, """{"customerId":"44444444-4444-4444-4444-444444444444"}""");
                await session.SendError(id, """{"errors":[{"message":"server-side boom"}]}""");
            }
        };

        await using var _ = client;
        await client.Start().WaitAsync(TimeSpan.FromSeconds(10));
        await WaitUntilAsync(() => errors.Count == 1);

        // The error arrives after the sentinel on one socket, so ordering guarantees the error
        // handling is complete here: exactly the sentinel was delivered as a payload.
        var sentinel = Assert.Single(payloads);
        Assert.Equal("44444444-4444-4444-4444-444444444444", sentinel.GetProperty("customerId").GetString());
        var error = Assert.Single(errors);
        Assert.Equal("server-side boom", Assert.Single(error.Errors).Message);
        Assert.Single(failures);
    }

    [Fact]
    public async Task CompleteFromServer_CompletesTheStream()
    {
        await using var server = await GraphQLWsTestServer.StartAsync();
        var graphClient = WsTestGraphClientFactory.Create(server);
        var (client, payloads, _, failures) = CreateClient(graphClient);
        var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Subscription.Subscribe(_ => { }, () => completed.TrySetResult(true));

        server.OnMessage = async (session, text) =>
        {
            if (text.Contains("subscribe"))
            {
                var id = GetSubscriptionId(text);
                await session.SendNext(id, """{"customerId":"44444444-4444-4444-4444-444444444444"}""");
                await session.SendComplete(id);
            }
        };

        await using var _ = client;
        await client.Start().WaitAsync(TimeSpan.FromSeconds(10));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Single(payloads);
        Assert.Empty(failures);
    }

    [Fact]
    public async Task PingMessage_IsAnsweredWithPong()
    {
        await using var server = await GraphQLWsTestServer.StartAsync();
        var graphClient = WsTestGraphClientFactory.Create(server);
        var (client, _, _, _) = CreateClient(graphClient);

        server.OnMessage = async (session, text) =>
        {
            if (text.Contains("connection_ack"))
            {
                return; // our own ack echo is not routed back anyway
            }

            if (text.Contains("subscribe"))
            {
                await session.SendPing();
            }
        };

        await using var _ = client;
        await client.Start().WaitAsync(TimeSpan.FromSeconds(10));
        await WaitForIncomingAsync(server, t => t.Contains("\"pong\""), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task EmptyFrame_TerminatesWithFailure()
    {
        await using var server = await GraphQLWsTestServer.StartAsync();
        var graphClient = WsTestGraphClientFactory.Create(server);
        var (client, _, _, failures) = CreateClient(graphClient);
        var failed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Failures.Subscribe(e => failed.TrySetResult(e));

        server.OnMessage = async (session, text) =>
        {
            if (text.Contains("subscribe"))
            {
                await session.SendEmptyFrame();
            }
        };

        await using var _ = client;
        await client.Start().WaitAsync(TimeSpan.FromSeconds(10));
        var failure = await failed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.IsType<InvalidDataException>(failure);
        Assert.Contains("empty", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MissingConnectionAck_TimesOutWithGraphQueryRequestException()
    {
        await using var server = await GraphQLWsTestServer.StartAsync();
        server.NeverAck = true;
        var graphClient = WsTestGraphClientFactory.Create(server);
        var (client, _, _, _) = CreateClient(graphClient);

        await using var __ = client;
        var exception = await Assert.ThrowsAsync<GraphQueryRequestException>(() =>
            client.Start().WaitAsync(TimeSpan.FromSeconds(20)));

        Assert.Contains("connection_ack", exception.Message);
        Assert.Contains("10 seconds", exception.Message);
    }

    [Fact]
    public async Task ReconnectBudgetExhausted_ReportsOneTerminalFailure()
    {
        // Connect once, then stop the server for good. Every retry must fail, and after the
        // budget is spent the subscription reports exactly one terminal error.
        var server = await GraphQLWsTestServer.StartAsync();
        var options = new GraphClientOptions
        {
            SubscriptionProtocol = SubscriptionProtocol.GraphQLWebSocket,
            SubscriptionRetryAttempts = 2,
            UseSafeMode = false,
        };
        var httpClient = new HttpClient { BaseAddress = new Uri(server.HttpBaseAddress) };
        var graphClient = new GraphClient(httpClient, Options.Create(options), new ServiceCollection().BuildServiceProvider());
        var (client, _, _, failures) = CreateClient(graphClient);
        var failed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Failures.Subscribe(e => failed.TrySetResult(e));

        await using var _ = client;
        await client.Start().WaitAsync(TimeSpan.FromSeconds(10));
        await WaitUntilAsync(() => server.Incoming.Any(m => m.Contains("connection_init")), 10);

        await server.DisposeAsync();

        // Backoff is 1-2 s then 2-3 s with jitter, plus connect time: 30 s is a generous bound.
        var failure = await failed.Task.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Contains("reconnect", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(failures);
    }
    [Fact]
    public async Task FatalCloseCode_TerminatesTheSubscription()
    {
        await using var server = await GraphQLWsTestServer.StartAsync();
        var graphClient = WsTestGraphClientFactory.Create(server);
        var (client, _, _, failures) = CreateClient(graphClient);
        var failed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Failures.Subscribe(e => failed.TrySetResult(e));

        server.OnMessage = async (session, text) =>
        {
            if (text.Contains("subscribe"))
            {
                await session.Close(4401, "Unauthorized");
            }
        };

        await using var _ = client;
        await client.Start().WaitAsync(TimeSpan.FromSeconds(10));
        var failure = await failed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.IsType<IOException>(failure);
        Assert.Contains("4401", failure.Message);
    }

    [Fact]
    public async Task TransientDrop_ReconnectsAndResumesDelivery()
    {
        await using var server = await GraphQLWsTestServer.StartAsync();
        var graphClient = WsTestGraphClientFactory.Create(server);
        var (client, payloads, errors, failures) = CreateClient(graphClient);
        var subscribeCount = 0;

        server.OnMessage = async (session, text) =>
        {
            if (!text.Contains("subscribe"))
            {
                return;
            }

            var id = GetSubscriptionId(text);
            var count = Interlocked.Increment(ref subscribeCount);
            if (count == 1)
            {
                // Drop the TCP connection without a close frame: recoverable.
                await Task.Delay(100);
                session.Abort();
                return;
            }

            // Second session (post-reconnect): deliver a real payload.
            await session.SendNext(id, """{"customerId":"55555555-5555-5555-5555-555555555555"}""");
        };

        await using var _ = client;
        await client.Start().WaitAsync(TimeSpan.FromSeconds(10));
        await WaitUntilAsync(() => payloads.Count == 1, 30);

        Assert.Empty(errors);
        Assert.Empty(failures);
        Assert.Single(payloads);
        Assert.Equal("55555555-5555-5555-5555-555555555555", payloads[0].GetProperty("customerId").GetString());
    }

    [Fact]
    public async Task ReconnectSucceedsAfterFailedAttempts()
    {
        // Drop the first session, refuse the next two upgrades, then accept: the third reconnect
        // attempt must open a real socket and resume delivery (failed attempts must not wedge the client).
        await using var server = await GraphQLWsTestServer.StartAsync();
        var graphClient = WsTestGraphClientFactory.Create(server);
        var (client, payloads, errors, failures) = CreateClient(graphClient);
        var subscribeCount = 0;

        server.OnMessage = async (session, text) =>
        {
            if (!text.Contains("subscribe"))
            {
                return;
            }

            var id = GetSubscriptionId(text);
            if (Interlocked.Increment(ref subscribeCount) == 1)
            {
                Interlocked.Exchange(ref server.RejectUpgrades, 2);
                await Task.Delay(100);
                session.Abort();
                return;
            }

            await session.SendNext(id, """{"customerId":"66666666-6666-6666-6666-666666666666"}""");
        };

        await using var _ = client;
        await client.Start().WaitAsync(TimeSpan.FromSeconds(10));
        // Backoff 1+2+4 s plus up to 3x3 s jitter: 40 s bounds three attempts.
        await WaitUntilAsync(() => payloads.Count == 1 || failures.Count > 0, 40);

        Assert.Empty(failures);
        Assert.Empty(errors);
        Assert.Equal(2, Volatile.Read(ref server.RejectedUpgrades));
        Assert.Equal("66666666-6666-6666-6666-666666666666", payloads[0].GetProperty("customerId").GetString());
    }
    [Fact]
    public async Task SilentConnection_IsDetected_AndReconnects()
    {
        // First session goes silent (no close, no frames): a half-open connection. The client must
        // notice the silence and reconnect; the second session delivers.
        await using var server = await GraphQLWsTestServer.StartAsync();
        var graphClient = WsTestGraphClientFactory.Create(server);
        var (client, payloads, errors, failures) = CreateClient(graphClient);
        client.NoMessageTimeout = TimeSpan.FromSeconds(2);
        var subscribeCount = 0;

        server.OnMessage = async (session, text) =>
        {
            if (!text.Contains("subscribe"))
            {
                return;
            }

            var id = GetSubscriptionId(text);
            if (Interlocked.Increment(ref subscribeCount) == 1)
            {
                return; // stay silent
            }

            await session.SendNext(id, """{"customerId":"77777777-7777-7777-7777-777777777777"}""");
        };

        await using var _ = client;
        await client.Start().WaitAsync(TimeSpan.FromSeconds(10));
        await WaitUntilAsync(() => payloads.Count == 1 || failures.Count > 0, 30);

        Assert.Empty(failures);
        Assert.Empty(errors);
        Assert.Equal(2, Volatile.Read(ref subscribeCount));
        Assert.Equal("77777777-7777-7777-7777-777777777777", payloads[0].GetProperty("customerId").GetString());
    }
    [Fact]
    public async Task BudgetExhausted_FailureIsReportedOutsideTheStateLock()
    {
        // Subscriber callbacks must not run while WSClient holds its internal state lock, or a
        // subscriber that touches the client from another thread (e.g. DisposeAsync) blocks on it.
        var server = await GraphQLWsTestServer.StartAsync();
        var options = new GraphClientOptions
        {
            SubscriptionProtocol = SubscriptionProtocol.GraphQLWebSocket,
            SubscriptionRetryAttempts = 0,
            UseSafeMode = false,
        };
        var httpClient = new HttpClient { BaseAddress = new Uri(server.HttpBaseAddress) };
        var graphClient = new GraphClient(httpClient, Options.Create(options), new ServiceCollection().BuildServiceProvider());
        var client = new WSClient(graphClient, new GraphQLRequest { Query = "subscription { customerAdded { customerId } }" });
        var stateLock = typeof(WSClient)
            .GetField("stateLock", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(client)!;
        var lockFreeInCallback = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Failures.Subscribe(_ =>
        {
            var free = Task.Run(() =>
            {
                if (!Monitor.TryEnter(stateLock, TimeSpan.FromSeconds(2)))
                {
                    return false;
                }

                Monitor.Exit(stateLock);
                return true;
            }).Result;
            lockFreeInCallback.TrySetResult(free);
        });

        await using var _ = client;
        await client.Start().WaitAsync(TimeSpan.FromSeconds(10));
        await server.DisposeAsync();

        Assert.True(await lockFreeInCallback.Task.WaitAsync(TimeSpan.FromSeconds(20)));
    }
    [Fact]
    public async Task ApolloProtocol_UsesLegacyStartAndDataMessages()
    {
        await using var server = await GraphQLWsTestServer.StartAsync();
        server.SubProtocol = "graphql-ws";
        var graphClient = WsTestGraphClientFactory.Create(server, SubscriptionProtocol.ApolloWebSocket);
        var (client, payloads, errors, failures) = CreateClient(graphClient);

        server.OnMessage = async (session, text) =>
        {
            if (text.Contains("\"start\""))
            {
                var id = GetSubscriptionId(text);
                var data = "{\"type\":\"data\",\"id\":\"" + id +
                           "\",\"payload\":{\"customerId\":\"66666666-6666-6666-6666-666666666666\"}}";
                await session.SendText(data);
            }
        };

        await using var _ = client;
        await client.Start().WaitAsync(TimeSpan.FromSeconds(10));
        await WaitForIncomingAsync(server, t => t.Contains("\"start\""), TimeSpan.FromSeconds(10));
        await WaitUntilAsync(() => payloads.Count == 1);

        Assert.Contains(server.Incoming, t => t.Contains("\"connection_init\""));
        Assert.Contains(server.Incoming, t => t.Contains("\"start\""));
        Assert.Single(payloads);
        Assert.Equal("66666666-6666-6666-6666-666666666666", payloads[0].GetProperty("customerId").GetString());
        Assert.Empty(errors);
        Assert.Empty(failures);
    }

    [Fact]
    public async Task MissingSubprotocol_FailsInitialConnect()
    {
        await using var server = await GraphQLWsTestServer.StartAsync();
        server.SubProtocol = "";
        var graphClient = WsTestGraphClientFactory.Create(server);
        var (client, _, _, _) = CreateClient(graphClient);

        await using var _ = client;
        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            client.Start().WaitAsync(TimeSpan.FromSeconds(10)));

        Assert.Contains("subprotocol", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
