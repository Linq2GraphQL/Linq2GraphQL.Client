using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Shouldly;

namespace Linq2GraphQL.Tests;

/// <summary>
///     LINQ operators other than Select inside a query expression. These run on the client, so the query has to
///     fetch the fields they read. Before the expression parser was rewritten they either threw or produced a
///     query that was missing fields.
/// </summary>
public class QueryOperatorTests : IClassFixture<SampleClientFixture>
{
    private readonly SampleClient sampleClient;

    public QueryOperatorTests(SampleClientFixture safeModeClient)
    {
        sampleClient = safeModeClient.sampleClient;
    }

    [Fact]
    public async Task First_WithoutLambda()
    {
        var query = sampleClient
            .Query
            .Orders()
            .Select(e => e.Nodes.First().OrderId);

        var request = await query.GetRequestAsync();
        request.Query.ShouldContain("orderId");

        (await query.ExecuteAsync()).ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Count_WithoutLambda()
    {
        var count = await sampleClient
            .Query
            .Orders()
            .Select(e => e.Nodes.Count())
            .ExecuteAsync();

        count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Where_FetchesThePredicateFields()
    {
        var query = sampleClient
            .Query
            .Orders()
            .Select(e => e.Nodes
                .Where(n => n.Customer.CustomerName != null)
                .Select(n => n.OrderId));

        var request = await query.GetRequestAsync();
        request.Query.ShouldContain("customerName");

        var result = await query.ExecuteAsync();
        result.ShouldAllBe(e => e != Guid.Empty);

        // The predicate fields are fetched, the rest of the order is not.
        query.BaseResult.Nodes.First().OrderDate.ShouldBe(default);
    }

    [Fact]
    public async Task OrderBy_FetchesTheKeyFields()
    {
        var query = sampleClient
            .Query
            .Orders()
            .Select(e => e.Nodes
                .OrderByDescending(n => n.OrderDate)
                .Select(n => n.OrderId));

        var request = await query.GetRequestAsync();
        request.Query.ShouldContain("orderDate");

        var result = (await query.ExecuteAsync()).ToList();
        var ordered = query.BaseResult.Nodes.OrderByDescending(e => e.OrderDate).Select(e => e.OrderId);

        result.ShouldBe(ordered);
    }

    [Fact]
    public async Task ChainedOperators()
    {
        var query = sampleClient
            .Query
            .Orders()
            .Select(e => e.Nodes
                .Where(n => n.OrderId != Guid.Empty)
                .OrderBy(n => n.OrderDate)
                .Take(1)
                .Select(n => new { n.OrderId, n.Customer.CustomerName }));

        var result = (await query.ExecuteAsync()).ToList();

        result.Count.ShouldBe(1);
        result[0].OrderId.ShouldNotBe(Guid.Empty);
        result[0].CustomerName.ShouldNotBeNull();
    }

    [Fact]
    public async Task SelectMany_WithResultSelector()
    {
        var query = sampleClient
            .Query
            .Customers()
            .Select(e => e.SelectMany(c => c.Orders, (c, o) => new { c.CustomerName, o.OrderId }));

        var result = (await query.ExecuteAsync()).ToList();

        result.ShouldNotBeEmpty();
        result.ShouldAllBe(e => e.CustomerName != null && e.OrderId != Guid.Empty);
    }

    [Fact]
    public async Task ToDictionary_FetchesTheKeyAndElementFields()
    {
        var query = sampleClient
            .Query
            .Orders()
            .Select(e => e.Nodes.ToDictionary(n => n.OrderId.ToString(), n => n.Customer.CustomerName,
                StringComparer.OrdinalIgnoreCase));

        var request = await query.GetRequestAsync();
        request.Query.ShouldContain("customerName");
        request.Query.ShouldContain("orderId");

        var result = await query.ExecuteAsync();

        result.ShouldNotBeEmpty();
        result.Values.ShouldAllBe(e => e != null);
    }

    [Fact]
    public async Task GroupBy_FetchesTheKeyFields()
    {
        var query = sampleClient
            .Query
            .Orders()
            .Select(e => e.Nodes
                .GroupBy(n => n.Customer.CustomerName, (name, orders) => orders.Select(o => o.OrderId)));

        var request = await query.GetRequestAsync();
        request.Query.ShouldContain("customerName");

        var result = (await query.ExecuteAsync()).ToList();

        result.ShouldNotBeEmpty();
        result.SelectMany(e => e).ShouldAllBe(e => e != Guid.Empty);
    }

    [Fact]
    public async Task UnsupportedOperator_IsReported()
    {
        var exception = Should.Throw<GraphQueryTranslationException>(() => sampleClient
            .Query
            .Orders()
            .Select(e => e.Nodes.Concat(e.Nodes)));

        exception.Message.ShouldContain("Concat");
        exception.Message.ShouldContain("ExecuteAsync");
        exception.MemberName.ShouldBe("Concat");
    }
}
