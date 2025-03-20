using Linq2GraphQL.TestClient;

namespace Linq2GraphQL.Tests;

public class QueryPagingTests : IClassFixture<SampleClientFixture>
{
    private readonly SampleClient sampleClient;

    public QueryPagingTests(SampleClientFixture sampleClient)
    {
        this.sampleClient = sampleClient.sampleClient;
    }


    [Fact]
    public async Task Orders_ManualPaging()
    {
        var query = sampleClient
            .Query
            .OrdersNoBackwardPagination(2)
            .Include(e => e.Nodes)
            .Include(e => e.PageInfo)
            .Include(e => e.TotalCount)
            .Select(e => e.Nodes.Select(e => new { e.OrderId, e.OrderDate }));

        var request = await query.GetRequestAsync();

        await query.ExecuteBaseAsync();

        var result1 = query.ConvertResult(query.BaseResult);
        var result2 = result1;

        if (query.BaseResult.PageInfo.HasNextPage)
        {
            query.QueryNode.SetArgumentValue("after", query.BaseResult.PageInfo.EndCursor);
            result2 = await query.ExecuteAsync();
        }

        Assert.NotEqual(result1.First().OrderId, result2.First().OrderId);
    }

    [Fact]
    public async Task Orders_NotIncludeTotalCount()
    {
        var query = sampleClient
            .Query
            .Orders()
            .Select(e => e.Nodes);

        var result = await query.ExecuteAsync();

        Assert.Equal(0, query.BaseResult.TotalCount);
    }


    [Fact]
    public async Task Orders_Paging()
    {
        var pager = sampleClient
            .Query
            .Orders()
            .Include(e => e.TotalCount)
            //.Include(e => e.Nodes.Select(e => e.Customer.Orders))
            .Select(e => e.Nodes.Select(e => new { e.OrderId }))
            .AsPager();

        var firstPage = await pager.NextPageAsync();
        var totalCount = pager.PagerResult.TotalCount;
        var secondPage = await pager.NextPageAsync();
        var firstPageAgain = await pager.PreviousPageAsync();


        Assert.NotEqual(firstPage.First().OrderId, secondPage.First().OrderId);
        Assert.Equal(firstPage.First().OrderId, firstPageAgain.First().OrderId);
    }
}