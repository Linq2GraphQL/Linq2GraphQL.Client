using System.Text.Json;
using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;

namespace Linq2GraphQL.Tests.Batch;

/// <summary>
/// Every query shape gives the same result whether it is executed on its own, alone in a batch, or in a batch
/// together with all the others.
/// </summary>
public class BatchEquivalenceTests : IDisposable
{
    private static readonly Dictionary<string, QueryCase> Cases = new[]
    {
        QueryCase.Of("Hello", c => c.Query.Hello("Jocke").Select()),
        QueryCase.Of("HelloDefault", c => c.Query.Hello().Select()),
        QueryCase.Of("TopLevelArgument", c => c.Query.Orders(1).Select(e => e.Nodes)),
        QueryCase.Of("ArgumentOnNestedField", c => c.Query.Orders(1)
            .Include(e => e.Nodes.Select(n => n.OrderAddress(AddressType.Invoice)))
            .Select(e => e.Nodes)),
        QueryCase.Of("SameFieldWithDifferentArguments", c => c.Query.Orders()
            .Select(e => e.Nodes.Select(n => new { A = n.OrderHello("A", 1), B = n.OrderHello("B", 2) }))),
        QueryCase.Of("IncludeMultipleLevels", c => c.Query.Orders(1)
            .Include(e => e.Nodes.Select(n => n.Customer.Orders))
            .Select(e => e.Nodes)),
        QueryCase.Of("LinqOperators", c => c.Query.Orders()
            .Select(e => e.Nodes.Where(n => n.OrderId != Guid.Empty).Select(n => new { n.OrderId, n.Customer.CustomerName }))),
        QueryCase.Of("FilterAndSort", c => c.Query.Orders(first: 2,
                where: new OrderFilterInput { OrderId = new UuidOperationFilterInput { Neq = Guid.Empty } },
                order: [new OrderSortInput { OrderDate = SortEnumType.Desc }])
            .Select(e => e.Nodes.Select(n => new { n.OrderId, n.OrderDate }))),
        QueryCase.Of("Paging", c => c.Query.Orders(first: 2).Select(e => new { e.PageInfo.HasNextPage, e.Nodes.Count })),
        QueryCase.Of("Interfaces", c => c.Query.Animals()
            .Include(e => e.Nodes.Select(n => n.Pig()))
            .Include(e => e.Nodes.Select(n => n.Spider()))
            .Select(e => e.Nodes)),
        QueryCase.Of("SelectMany", c => c.Query.Customers()
            .Select(e => e.SelectMany(cu => cu.Orders, (cu, o) => new { cu.CustomerName, o.OrderId }))),
        QueryCase.Of("NullResult", c => c.Query.CustomerReturnNull().Select())
    }.ToDictionary(c => c.Name);

    private readonly BatchTestServer server = new();
    private readonly SampleClient client;

    public BatchEquivalenceTests()
    {
        client = server.CreateClient();
    }

    public void Dispose()
    {
        server.Dispose();
    }

    public static TheoryData<string> CaseNames => new(Cases.Keys);

    [Theory]
    [MemberData(nameof(CaseNames))]
    public async Task AloneInABatch_GivesTheStandaloneResult(string caseName)
    {
        var queryCase = Cases[caseName];
        var standalone = await queryCase.ExecuteAsync(client);
        string? batched = null;

        await client.CreateBatch().Query(c => queryCase.ToItem(c, r => batched = r)).ExecuteAsync();

        Assert.Equal(standalone, batched);
    }

    [Theory]
    [InlineData(GraphBatchMode.SingleRequest, 100)]
    [InlineData(GraphBatchMode.SingleRequest, 3)]
    [InlineData(GraphBatchMode.Parallel, 100)]
    public async Task TogetherInOneBatch_EachGivesItsStandaloneResult(GraphBatchMode mode, int maxItemsPerRequest)
    {
        var standalone = new Dictionary<string, string>();
        foreach (var queryCase in Cases.Values)
        {
            standalone[queryCase.Name] = await queryCase.ExecuteAsync(client);
        }

        var batched = new Dictionary<string, string>();
        var batch = client.CreateBatch(o =>
        {
            o.Mode = mode;
            o.MaxItemsPerRequest = maxItemsPerRequest;
        });
        foreach (var queryCase in Cases.Values)
        {
            batch.Query(c => queryCase.ToItem(c, r => batched[queryCase.Name] = r));
        }

        await batch.ExecuteAsync();

        Assert.Equal(standalone, batched);
    }

    private sealed record QueryCase(
        string Name,
        Func<SampleClient, Task<string>> ExecuteAsync,
        Func<SampleClient, Action<string>, GraphBatchItem> ToItem)
    {
        public static QueryCase Of<T, TResult>(string name, Func<SampleClient, GraphQueryExecute<T, TResult>> build)
        {
            return new QueryCase(name,
                async c => JsonSerializer.Serialize(await build(c).ExecuteAsync()),
                (c, onResult) => build(c).OnResult(r => onResult(JsonSerializer.Serialize(r))));
        }
    }
}
