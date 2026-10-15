using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Covers GraphClient.ExecuteAsync for queries written as GraphQL text, through the generated client's
/// GraphClient property.
/// </summary>
public class RawExecutionTests : IClassFixture<SampleClientFixture>
{
    private readonly SampleClient sampleClient;

    public RawExecutionTests(SampleClientFixture fixture)
    {
        sampleClient = fixture.sampleClient;
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsTheWholeDataObject()
    {
        var result = await sampleClient.GraphClient.ExecuteAsync("query { hello(name: \"Jocke\") }");

        Assert.False(result.HasErrors);
        Assert.Equal("Hello, Jocke!", result.Data.GetProperty("hello").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_SendsVariables()
    {
        var result = await sampleClient.GraphClient.ExecuteAsync(
            "query($name: String) { hello(name: $name) }",
            new Dictionary<string, object> { ["name"] = "Magnus" });

        Assert.Equal("Hello, Magnus!", result.Data.GetProperty("hello").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_InvalidDocument_ReturnsErrorsInsteadOfThrowing()
    {
        var result = await sampleClient.GraphClient.ExecuteAsync("query { doesNotExist }");

        Assert.True(result.HasErrors);
        Assert.Contains(result.Errors, e => e.Message.Contains("doesNotExist"));
    }

    [Fact]
    public async Task ExecuteAsync_ResolverError_ReturnsErrorsAndData()
    {
        var result = await sampleClient.GraphClient.ExecuteAsync("query { raiseError(message: \"boom\") }");

        Assert.True(result.HasErrors);
        Assert.Contains(result.Errors, e => e.Message.Contains("boom"));
    }

    [Fact]
    public async Task ExecuteAsync_GoesThroughTheDerivedClientHooks()
    {
        string? authorization = null;
        var userClient = sampleClient.WithRequestConfiguration((request, _) =>
        {
            request.Headers.Authorization = new("Bearer", "user-token");
            authorization = request.Headers.Authorization.ToString();
            return ValueTask.CompletedTask;
        });

        var result = await userClient.GraphClient.ExecuteAsync("query { hello }");

        Assert.False(result.HasErrors);
        Assert.Equal("Bearer user-token", authorization);
    }
}
