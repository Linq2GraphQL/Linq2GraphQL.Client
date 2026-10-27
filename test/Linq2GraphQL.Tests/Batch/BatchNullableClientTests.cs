using Linq2GraphQL.Client;

namespace Linq2GraphQL.Tests.Batch;

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
