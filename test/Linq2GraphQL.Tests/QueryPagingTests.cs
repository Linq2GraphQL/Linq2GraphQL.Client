using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

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

    [Fact]
    public async Task Orders_PreviousPage_ReturnsPageBeforeCurrent()
    {
        var pager = sampleClient
            .Query
            .Orders(first: 1)
            .Select(e => e.Nodes.Select(e => new { e.OrderId }))
            .AsPager();

        await pager.NextPageAsync();
        var secondPage = await pager.NextPageAsync();
        var thirdPage = await pager.NextPageAsync();
        var secondPageAgain = await pager.PreviousPageAsync();
        var thirdPageAgain = await pager.NextPageAsync();

        Assert.Equal(secondPage.Select(e => e.OrderId), secondPageAgain.Select(e => e.OrderId));
        Assert.Equal(thirdPage.Select(e => e.OrderId), thirdPageAgain.Select(e => e.OrderId));
    }

    [Fact]
    public async Task Orders_PreviousPageWithResult_ReturnsPageBeforeCurrent()
    {
        var pager = sampleClient
            .Query
            .Orders(first: 1)
            .Select(e => e.Nodes.Select(e => new { e.OrderId }))
            .AsPager();

        await pager.NextPageWithResultAsync();
        var secondPage = await pager.NextPageWithResultAsync();
        await pager.NextPageWithResultAsync();
        var secondPageAgain = await pager.PreviousPageWithResultAsync();

        Assert.Equal(secondPage.Data.Select(e => e.OrderId), secondPageAgain.Data.Select(e => e.OrderId));
    }

    [Fact]
    public async Task Orders_GetAllPages_ReturnsEveryPage()
    {
        var pager = sampleClient
            .Query
            .Orders(first: 1)
            .Include(e => e.TotalCount)
            .Select(e => e.Nodes.Select(e => new { e.OrderId }))
            .AsPager();

        var pages = await pager.GetAllPagesAsync().ToListAsync();
        var orderIds = pages.SelectMany(e => e).Select(e => e.OrderId).ToList();

        Assert.Equal(pager.PagerResult.TotalCount, pages.Count);
        Assert.Equal(pages.Count, orderIds.Distinct().Count());
        Assert.False(pager.PagerResult.PageInfo.HasNextPage);
    }

    [Fact]
    public async Task Orders_GetAllPages_UsesServerDefaultPageSize()
    {
        var pager = sampleClient
            .Query
            .Orders()
            .Include(e => e.TotalCount)
            .Select(e => e.Nodes.Select(e => new { e.OrderId }))
            .AsPager();

        var orderIds = await pager.GetAllPagesAsync().SelectMany(e => e.ToAsyncEnumerable()).ToListAsync();

        Assert.Equal(pager.PagerResult.TotalCount, orderIds.Count);
        Assert.Equal(orderIds.Count, orderIds.Distinct().Count());
    }

    [Fact]
    public async Task Orders_GetAllPages_StartsFromFirstPage()
    {
        var pager = sampleClient
            .Query
            .Orders(first: 1)
            .Select(e => e.Nodes.Select(e => new { e.OrderId }))
            .AsPager();

        var firstPage = await pager.NextPageAsync();
        await pager.NextPageAsync();

        var pages = await pager.GetAllPagesAsync().ToListAsync();

        Assert.Equal(firstPage.Select(e => e.OrderId), pages.First().Select(e => e.OrderId));
    }

    [Fact]
    public async Task Orders_GetAllPages_StopsAtMaxPages()
    {
        var pager = sampleClient
            .Query
            .Orders(first: 1)
            .Select(e => e.Nodes.Select(e => new { e.OrderId }))
            .AsPager();

        var pages = await pager.GetAllPagesAsync(maxPages: 2).ToListAsync();

        Assert.Equal(2, pages.Count);
        Assert.True(pager.PagerResult.PageInfo.HasNextPage);
    }

    [Fact]
    public async Task Orders_GetAllPages_MaxPagesBelowOne_Throws()
    {
        var pager = sampleClient
            .Query
            .Orders(first: 1)
            .Select(e => e.Nodes.Select(e => new { e.OrderId }))
            .AsPager();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await pager.GetAllPagesAsync(maxPages: 0).ToListAsync());
    }

    [Fact]
    public async Task Orders_GetAllPagesWithResult_ReturnsEveryPage()
    {
        var pager = sampleClient
            .Query
            .Orders(first: 1)
            .Include(e => e.TotalCount)
            .Select(e => e.Nodes.Select(e => new { e.OrderId }))
            .AsPager();

        var pages = await pager.GetAllPagesWithResultAsync().ToListAsync();

        Assert.Equal(pager.PagerResult.TotalCount, pages.Count);
        Assert.All(pages, e => Assert.False(e.HasErrors));
        Assert.Equal(pages.Count, pages.SelectMany(e => e.Data).Select(e => e.OrderId).Distinct().Count());
    }

    [Fact]
    public async Task GetAllPages_EndCursorDoesNotAdvance_Stops()
    {
        // A server that always claims there is a next page but never moves its cursor.
        const string response = """
            {"data":{"orders":{"pageInfo":{"hasNextPage":true,"hasPreviousPage":false,"startCursor":"MA==","endCursor":"MA=="},"nodes":[]}}}
            """;
        var httpClient = new HttpClient(new StaticResponseHandler(response))
        {
            BaseAddress = new Uri("https://localhost/graphql/")
        };
        var client = new SampleClient(httpClient, Options.Create(new GraphClientOptions()),
            new ServiceCollection().BuildServiceProvider());

        var pager = client
            .Query
            .Orders(first: 1)
            .Select(e => e.Nodes.Select(e => new { e.OrderId }))
            .AsPager();

        var pages = await pager.GetAllPagesAsync(maxPages: 10).ToListAsync();

        Assert.Equal(2, pages.Count);
    }

    private sealed class StaticResponseHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}