using System.Collections.Concurrent;
using System.Text.Json;
using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.Tests.Batch;

/// <summary>The in-memory test server, with every request the client sends recorded.</summary>
internal sealed class BatchTestServer : IDisposable
{
    private readonly WebApplicationFactory<Program> application = new();
    private readonly WebApplicationFactory<Program> configuredApplication;
    private readonly RecordingHandler recorder;
    private readonly HttpClient httpClient;

    public BatchTestServer(Action<IServiceCollection>? configureServer = null)
    {
        configuredApplication = configureServer == null
            ? application
            : application.WithWebHostBuilder(builder => builder.ConfigureServices(configureServer));
        recorder = new RecordingHandler { InnerHandler = configuredApplication.Server.CreateHandler() };
        httpClient = new HttpClient(recorder) { BaseAddress = new Uri("https://localhost:7184/graphql/") };
    }

    /// <summary>The operations sent, schema queries left out, in the order they were sent.</summary>
    public IReadOnlyList<RecordedOperation> Operations => recorder.Operations.Where(o => !o.IsSchemaQuery).ToList();

    public int SchemaQueryCount => recorder.Operations.Count(o => o.IsSchemaQuery);

    public SampleClient CreateClient(bool useSafeMode = true)
    {
        return new SampleClient(httpClient, Options.Create(new GraphClientOptions { UseSafeMode = useSafeMode }),
            new ServiceCollection().AddMemoryCache().BuildServiceProvider());
    }

    public void Dispose()
    {
        httpClient.Dispose();
        configuredApplication.Dispose();
        application.Dispose();
    }

    private sealed class RecordingHandler : DelegatingHandler
    {
        public ConcurrentQueue<RecordedOperation> Operations { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            Operations.Enqueue(RecordedOperation.Parse(body, request.Headers));
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

internal sealed record RecordedOperation(string Query, string Variables, IReadOnlyList<string> BatchHeader)
{
    public bool IsSchemaQuery => Query.Contains("__schema");

    public static RecordedOperation Parse(string body, System.Net.Http.Headers.HttpRequestHeaders headers)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var variables = root.TryGetProperty("variables", out var value) ? value.GetRawText() : "null";
        return new RecordedOperation(root.GetProperty("query").GetString()!, variables,
            headers.TryGetValues("X-Batch", out var header) ? header.ToList() : []);
    }
}
