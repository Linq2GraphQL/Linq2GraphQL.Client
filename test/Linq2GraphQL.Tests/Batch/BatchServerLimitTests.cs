using HotChocolate.Execution.Configuration;
using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Microsoft.Extensions.DependencyInjection;

namespace Linq2GraphQL.Tests.Batch;

/// <summary>Batches against the real test server with a parser limit that a large batch exceeds.</summary>
public class BatchServerLimitTests : IDisposable
{
    private const int MaxAllowedFields = 12;

    private readonly BatchTestServer server = new(services =>
        services.AddGraphQLServer().ModifyParserOptions(o => o.MaxAllowedFields = MaxAllowedFields));

    public void Dispose()
    {
        server.Dispose();
    }

    [Fact]
    public async Task BatchOverTheLimit_IsSplit_AndEveryItemGetsItsResult()
    {
        var client = server.CreateClient(useSafeMode: false);
        var results = new List<string>();
        var batch = client.CreateBatch();
        for (var i = 0; i < 30; i++)
        {
            var name = "n" + i;
            batch.Query(c => c.Query.Hello(name).Select().OnResult(r => results.Add(r)));
        }

        await batch.ExecuteAsync();

        Assert.Equal(Enumerable.Range(0, 30).Select(i => $"Hello, n{i}!"), results);
        Assert.True(server.Operations.Count > 1);
        Assert.NotNull(client.GraphClient.AcceptedBatchSize);
        Assert.True(client.GraphClient.AcceptedBatchSize <= MaxAllowedFields);
    }

    [Fact]
    public async Task WithoutSplitting_TheServersRejectionIsReported()
    {
        var client = server.CreateClient(useSafeMode: false);
        var batch = client.CreateBatch(o => o.SplitOnRejection = false);
        for (var i = 0; i < 30; i++)
        {
            batch.Query(c => c.Query.Hello().Select().OnResult(_ => { }));
        }

        var result = await batch.ExecuteWithResultAsync();

        Assert.True(result.HasErrors);
        Assert.Single(server.Operations);
    }
}

public class BatchNullableClientTests : IClassFixture<SampleNullableClientFixture>
{
    private readonly TestClientNullable.SampleNullableClient client;

    public BatchNullableClientTests(SampleNullableClientFixture fixture)
    {
        client = fixture.sampleClient;
    }

    [Fact]
    public async Task NullableClient_BatchGivesTheStandaloneResults()
    {
        var expectedList = await client.Query.CustomerListAllNullable().Select().ExecuteAsync();
        var expectedNested = await client.Query.CustomerListInList().Select().ExecuteAsync();
        List<TestClientNullable.Customer?>? list = null;
        List<List<TestClientNullable.Customer>>? nested = null;

        await client.CreateBatch()
            .Query(c => c.Query.CustomerListAllNullable().Select().OnResult(r => list = r))
            .Query(c => c.Query.CustomerListInList().Select().OnResult(r => nested = r))
            .ExecuteAsync();

        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(expectedList), System.Text.Json.JsonSerializer.Serialize(list));
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(expectedNested), System.Text.Json.JsonSerializer.Serialize(nested));
    }
}
