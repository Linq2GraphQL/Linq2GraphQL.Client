using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Linq2GraphQL.TestClientNullable;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.Compat;

/// <summary>
/// What one scenario gets: clients over the in-memory test servers or over a canned response, each recording the
/// requests it sends. Every client has its own schema cache, so safe mode runs its introspection query each time.
/// </summary>
internal sealed partial class CompatContext(
    WebApplicationFactory<Program> server,
    WebApplicationFactory<ProgramNullable> nullableServer)
{
    private static readonly Uri ServerAddress = new("https://localhost:7184/graphql/");
    private static readonly Uri NullableServerAddress = new("https://localhost:50741/graphql/");
    private static readonly Uri StubAddress = new("https://stub.local/graphql/");

    private readonly List<JsonObject> requests = [];

    public JsonArray Requests
    {
        get
        {
            lock (requests)
            {
                return new JsonArray(requests.Select(request => (JsonNode)request.DeepClone()).ToArray());
            }
        }
    }

    public SampleClient Client(GraphClientOptions options = null)
    {
        return new SampleClient(NewHttpClient(server.Server.CreateHandler(), ServerAddress),
            Options.Create(options ?? new GraphClientOptions()), NewServices());
    }

    public SampleClient SafeModeClient()
    {
        return Client(new GraphClientOptions
        {
            UseSafeMode = true, SubscriptionProtocol = SubscriptionProtocol.ServerSentEvents
        });
    }

    public SampleNullableClient NullableClient()
    {
        return new SampleNullableClient(NewHttpClient(nullableServer.Server.CreateHandler(), NullableServerAddress),
            Options.Create(new GraphClientOptions()), NewServices());
    }

    /// <summary>A client registered with AddSampleClient, the way an app wires it up.</summary>
    public SampleClient RegisteredClient()
    {
        var services = new ServiceCollection();
        services.AddSampleClient(options => options.UseSafeMode = true)
            .WithHttpClient(client => client.BaseAddress = ServerAddress,
                builder => builder.ConfigurePrimaryHttpMessageHandler(() =>
                    new RecordingHandler(this) { InnerHandler = server.Server.CreateHandler() }));
        return services.BuildServiceProvider().GetRequiredService<SampleClient>();
    }

    /// <summary>A client whose every request gets this response.</summary>
    public SampleClient StubClient(HttpStatusCode status, string body, string mediaType = "application/json")
    {
        return new SampleClient(NewHttpClient(new StubHandler(status, body, mediaType), StubAddress),
            Options.Create(new GraphClientOptions()), NewServices());
    }

    /// <summary>
    /// Sends a mutation past the recorder. Subscriptions use it to raise events without the retries, whose number
    /// depends on timing, ending up in the record.
    /// </summary>
    public async Task SendUnrecordedAsync(string query)
    {
        using var client = server.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = ServerAddress });
        var body = new JsonObject { ["query"] = query }.ToJsonString();
        using var response = await client.PostAsync("", new StringContent(body, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    private HttpClient NewHttpClient(HttpMessageHandler handler, Uri baseAddress)
    {
        return new HttpClient(new RecordingHandler(this) { InnerHandler = handler }) { BaseAddress = baseAddress };
    }

    private static IServiceProvider NewServices()
    {
        return new ServiceCollection().AddMemoryCache().BuildServiceProvider();
    }

    private sealed partial class RecordingHandler(CompatContext context) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string body = null;
            if (request.Content != null)
            {
                // Without the token: the transport below, not the recorder, must be what reacts to cancellation.
                await request.Content.LoadIntoBufferAsync();
                body = WithUnixLineBreaks(await request.Content.ReadAsStringAsync());
            }

            var headers = new JsonObject();
            var allHeaders = request.Headers.Concat(request.Content?.Headers ??
                Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>());
            foreach (var header in allHeaders.OrderBy(header => header.Key, StringComparer.OrdinalIgnoreCase))
            {
                headers[header.Key] = WithoutLibraryVersion(string.Join(", ", header.Value));
            }

            var record = new JsonObject
            {
                ["method"] = request.Method.Method,
                ["uri"] = request.RequestUri?.ToString(),
                ["headers"] = headers,
                ["body"] = body
            };

            lock (context.requests)
            {
                context.requests.Add(record);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// A package built on Windows sends CRLF wherever its query text spans source lines, one built elsewhere LF;
        /// GraphQL reads both alike.
        /// </summary>
        private static string WithUnixLineBreaks(string body)
        {
            return body.Replace(@"\r\n", @"\n");
        }

        private static string WithoutLibraryVersion(string value)
        {
            return LibraryVersion().Replace(value, "Linq2GraphQL/{version}");
        }

        [GeneratedRegex(@"Linq2GraphQL/[^\s,]+")]
        private static partial Regex LibraryVersion();
    }

    private sealed class StubHandler(HttpStatusCode status, string body, string mediaType) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, mediaType), RequestMessage = request
            });
        }
    }
}
