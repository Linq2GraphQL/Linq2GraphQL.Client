using System.Net.Http.Headers;
using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.Tests;

public class GraphClientOptionsTests : IDisposable
{
    private static readonly Uri BaseAddress = new("https://localhost:7184/graphql/");

    private readonly WebApplicationFactory<Program> application = new();
    private readonly RecordingHandler recorder;
    private readonly HttpClient httpClient;
    private readonly IServiceProvider services = new ServiceCollection().AddMemoryCache().BuildServiceProvider();

    public GraphClientOptionsTests()
    {
        recorder = new RecordingHandler { InnerHandler = application.Server.CreateHandler() };
        httpClient = new HttpClient(recorder) { BaseAddress = BaseAddress };
    }

    public void Dispose()
    {
        httpClient.Dispose();
        application.Dispose();
    }

    [Fact]
    public async Task ConfigureRequest_IsAppliedToQueryAndSchemaRequests()
    {
        var client = new SampleClient(httpClient, Options.Create(new GraphClientOptions
        {
            UseSafeMode = true,
            ConfigureRequest = (request, _) =>
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "agent-token");
                return ValueTask.CompletedTask;
            }
        }), services);

        var result = await client.Query.Hello("Jocke").Select().ExecuteAsync();

        Assert.Equal("Hello, Jocke!", result);
        Assert.Equal(2, recorder.Requests.Count);
        Assert.All(recorder.Requests, r => Assert.Equal("Bearer agent-token", r.Authorization));
    }

    [Fact]
    public async Task ConfigureRequest_IsCalledForEveryRequest()
    {
        var calls = 0;
        var client = new SampleClient(httpClient, Options.Create(new GraphClientOptions
        {
            ConfigureRequest = (request, _) =>
            {
                calls++;
                request.Headers.Add("X-Call", calls.ToString());
                return ValueTask.CompletedTask;
            }
        }), services);

        await client.Query.Hello().Select().ExecuteAsync();
        await client.Query.Hello().Select().ExecuteAsync();

        Assert.Equal(2, calls);
        Assert.Equal(["1", "2"], recorder.Requests.Select(r => r.CallHeader));
    }

    [Fact]
    public async Task ConfigureRequest_NotSet_SendsRequestUnchanged()
    {
        var client = new SampleClient(httpClient, Options.Create(new GraphClientOptions()), services);

        var result = await client.Query.Hello().Select().ExecuteAsync();

        Assert.Equal("Hello, World!", result);
        Assert.Null(Assert.Single(recorder.Requests).Authorization);
    }

    [Fact]
    public async Task SchemaCache_SameKey_IntrospectsOnce()
    {
        var first = new GraphClient(httpClient, SafeModeOptions(schemaCacheKey: null), services);
        var second = new GraphClient(httpClient, SafeModeOptions(schemaCacheKey: null), services);

        await first.GetSchemaForSafeModeAsync();
        await second.GetSchemaForSafeModeAsync();

        Assert.Equal(1, recorder.SchemaRequestCount);
    }

    [Fact]
    public async Task SchemaCache_DifferentSchemaCacheKey_IntrospectsPerKey()
    {
        var user = new GraphClient(httpClient, SafeModeOptions(schemaCacheKey: "user"), services);
        var agent = new GraphClient(httpClient, SafeModeOptions(schemaCacheKey: "agent"), services);

        await user.GetSchemaForSafeModeAsync();
        await agent.GetSchemaForSafeModeAsync();

        Assert.Equal(2, recorder.SchemaRequestCount);
    }

    [Fact]
    public async Task SchemaCache_DifferentIncludeDeprecated_IntrospectsPerSetting()
    {
        var withoutDeprecated = new GraphClient(httpClient, SafeModeOptions(schemaCacheKey: null), services, false);
        var withDeprecated = new GraphClient(httpClient, SafeModeOptions(schemaCacheKey: null), services, true);

        await withoutDeprecated.GetSchemaForSafeModeAsync();
        await withDeprecated.GetSchemaForSafeModeAsync();

        Assert.Equal(2, recorder.SchemaRequestCount);
    }

    private static IOptions<GraphClientOptions> SafeModeOptions(string? schemaCacheKey) =>
        Options.Create(new GraphClientOptions { UseSafeMode = true, SchemaCacheKey = schemaCacheKey });

    private sealed record RecordedRequest(string? Authorization, string? CallHeader, bool IsSchemaRequest);

    private sealed class RecordingHandler : DelegatingHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        public int SchemaRequestCount => Requests.Count(r => r.IsSchemaRequest);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(
                request.Headers.Authorization?.ToString(),
                request.Headers.TryGetValues("X-Call", out var values) ? values.Single() : null,
                body.Contains("__schema")));

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
