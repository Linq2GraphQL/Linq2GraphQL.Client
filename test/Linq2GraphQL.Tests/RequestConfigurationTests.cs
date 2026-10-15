using System.Net.Http.Headers;
using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Covers derived clients from WithRequestConfiguration / WithAccessToken: per-user headers on top of the
/// client-wide ConfigureRequest, without changing the client they were derived from.
/// </summary>
public class RequestConfigurationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> application = new();
    private readonly RecordingHandler recorder;
    private readonly HttpClient httpClient;
    private readonly IServiceProvider services = new ServiceCollection().AddMemoryCache().BuildServiceProvider();

    public RequestConfigurationTests()
    {
        recorder = new RecordingHandler { InnerHandler = application.Server.CreateHandler() };
        httpClient = new HttpClient(recorder) { BaseAddress = new Uri("https://localhost:7184/graphql/") };
    }

    public void Dispose()
    {
        httpClient.Dispose();
        application.Dispose();
    }

    private SampleClient NewClient(GraphClientOptions? options = null) =>
        new(httpClient, Options.Create(options ?? new GraphClientOptions()), services);

    [Fact]
    public async Task WithAccessToken_TypedQueriesSendTheToken()
    {
        var userClient = NewClient().WithAccessToken(_ => ValueTask.FromResult("user-token"));

        var result = await userClient.Query.Hello("Jocke").Select().ExecuteAsync();

        Assert.Equal("Hello, Jocke!", result);
        Assert.Equal("Bearer user-token", Assert.Single(recorder.Requests).Authorization);
    }

    [Fact]
    public async Task WithAccessToken_DoesNotChangeTheOriginalClient()
    {
        var client = NewClient();
        _ = client.WithAccessToken(_ => ValueTask.FromResult("user-token"));

        await client.Query.Hello().Select().ExecuteAsync();

        Assert.Null(Assert.Single(recorder.Requests).Authorization);
    }

    [Fact]
    public async Task WithAccessToken_EachDerivedClientSendsItsOwnToken()
    {
        var client = NewClient();
        var alice = client.WithAccessToken(_ => ValueTask.FromResult("alice"));
        var bob = client.WithAccessToken(_ => ValueTask.FromResult("bob"));

        await alice.Query.Hello().Select().ExecuteAsync();
        await bob.Query.Hello().Select().ExecuteAsync();

        Assert.Equal(["Bearer alice", "Bearer bob"], recorder.Requests.Select(r => r.Authorization));
    }

    [Fact]
    public async Task WithRequestConfiguration_RunsAfterOptionsHook_AndNewestWins()
    {
        var client = NewClient(new GraphClientOptions
        {
            ConfigureRequest = (request, _) =>
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "app-token");
                request.Headers.Add("X-Order", "options");
                return ValueTask.CompletedTask;
            }
        });

        var derived = client
            .WithRequestConfiguration((request, _) =>
            {
                request.Headers.Add("X-Order", "first");
                return ValueTask.CompletedTask;
            })
            .WithAccessToken(_ => ValueTask.FromResult("user-token"));

        await derived.Query.Hello().Select().ExecuteAsync();

        var request = Assert.Single(recorder.Requests);
        Assert.Equal("Bearer user-token", request.Authorization);
        Assert.Equal(["options", "first"], request.Order);
    }

    [Fact]
    public async Task WithAccessToken_IsAskedForTheTokenOnEveryRequest()
    {
        var calls = 0;
        var userClient = NewClient().WithAccessToken(_ => ValueTask.FromResult($"token-{++calls}"));

        await userClient.Query.Hello().Select().ExecuteAsync();
        await userClient.Query.Hello().Select().ExecuteAsync();

        Assert.Equal(["Bearer token-1", "Bearer token-2"], recorder.Requests.Select(r => r.Authorization));
    }

    private sealed record RecordedRequest(string? Authorization, string[] Order);

    private sealed class RecordingHandler : DelegatingHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new RecordedRequest(
                request.Headers.Authorization?.ToString(),
                request.Headers.TryGetValues("X-Order", out var values) ? values.ToArray() : []));

            return base.SendAsync(request, cancellationToken);
        }
    }
}
