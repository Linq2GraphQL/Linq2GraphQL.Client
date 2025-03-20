using Linq2GraphQL.TestClient;

namespace Linq2GraphQL.Tests;

public class QueryInterfaceTests : IClassFixture<SampleClientFixture>
{
    private readonly SampleClient sampleClient;

    public QueryInterfaceTests(SampleClientFixture safeModeClient)
    {
        sampleClient = safeModeClient.sampleClient;
    }

    [Fact]
    public async Task Interface_NoSubclass()
    {
        var result = await sampleClient
            .Query
            .Animals()
            .Select(e => e.Nodes)
            .ExecuteAsync();

        var pig = result.First();
        var spider = result.Last();

        Assert.IsType<Spider>(spider);
        Assert.IsType<Pig>(pig);
    }

    [Fact]
    public async Task Interface_QuerySubClass()
    {
        var query = sampleClient
            .Query
            .Animals()
            .Include(e => e.Nodes.Select(e => e.Pig()))
            .Include(e => e.Nodes.Select(e => e.Spider()))
            .Select(e => e.Nodes);


        var request = await query.GetRequestAsync();
        var result = await query.ExecuteAsync();

        var pig = result.First();
        var spider = result.Last();

        Assert.IsType<Spider>(spider);
        Assert.IsType<Pig>(pig);
    }
}