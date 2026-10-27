using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Golden copies of the request (query text and variables) a single query sends. Any change to them changes what
/// existing clients send to their servers, so a diff here must be deliberate.
/// </summary>
public class RequestSnapshotTests : IClassFixture<SampleClientFixture>, IClassFixture<SampleNullableClientFixture>
{
    private static readonly Guid FixedId = Guid.Parse("0190a4c2-7b3e-7c1a-9d2f-3e4b5a6c7d8e");

    private readonly SampleClient safeModeClient;
    private readonly SampleClient client;
    private readonly TestClientNullable.SampleNullableClient nullableClient;

    public RequestSnapshotTests(SampleClientFixture safeModeFixture, SampleNullableClientFixture nullableFixture)
    {
        safeModeClient = safeModeFixture.sampleClient;
        nullableClient = nullableFixture.sampleClient;
        client = new SampleClient(new HttpClient { BaseAddress = new Uri("https://localhost/graphql/") },
            Options.Create(new GraphClientOptions()), new ServiceCollection().BuildServiceProvider());
    }

    [Fact]
    public Task Hello_WithoutArguments() =>
        MatchesSnapshot(safeModeClient.Query.Hello().Select(), "Hello_WithoutArguments");

    [Fact]
    public Task Hello_WithArgument() =>
        MatchesSnapshot(safeModeClient.Query.Hello("Magnus").Select(), "Hello_WithArgument");

    [Fact]
    public Task Orders_TopLevelArgument() =>
        MatchesSnapshot(safeModeClient.Query.Orders(1).Select(e => e.Nodes), "Orders_TopLevelArgument");

    [Fact]
    public Task Orders_ArgumentOnNestedField() =>
        MatchesSnapshot(safeModeClient.Query.Orders(1)
            .Include(e => e.Nodes.Select(n => n.OrderAddress(AddressType.Invoice)))
            .Select(e => e.Nodes), "Orders_ArgumentOnNestedField");

    [Fact]
    public Task Orders_SameArgumentNameAtTwoLevels() =>
        MatchesSnapshot(safeModeClient.Query.Orders(1)
            .Include(e => e.Nodes.Select(n => n.OrderHello("Jocke", 2)))
            .Include(e => e.Nodes.Select(n => n.OrderAddress(AddressType.Invoice)))
            .Select(e => e.Nodes), "Orders_SameArgumentNameAtTwoLevels");

    [Fact]
    public Task Orders_SameFieldWithDifferentArguments() =>
        MatchesSnapshot(safeModeClient.Query.Orders()
            .Select(e => e.Nodes.Select(n => new { Jocke = n.OrderHello("Jocke", 22), Kalle = n.OrderHello("Kalle", 11) })),
            "Orders_SameFieldWithDifferentArguments");

    [Fact]
    public Task Orders_IncludeMultipleLevels() =>
        MatchesSnapshot(safeModeClient.Query.Orders(1)
            .Include(e => e.Nodes.Select(n => n.Customer))
            .Include(e => e.Nodes.Select(n => n.Customer.Orders))
            .Select(e => e.Nodes), "Orders_IncludeMultipleLevels");

    [Fact]
    public Task Orders_ProjectionWithLinqOperators() =>
        MatchesSnapshot(safeModeClient.Query.Orders()
            .Select(e => e.Nodes
                .Where(n => n.OrderId != Guid.Empty)
                .Select(n => new { n.OrderId, n.Customer.CustomerName })), "Orders_ProjectionWithLinqOperators");

    [Fact]
    public Task Orders_FilterAndSortInputs() =>
        MatchesSnapshot(safeModeClient.Query.Orders(first: 2, after: "cursor",
                where: new OrderFilterInput
                {
                    Customer = new CustomerFilterInput { CustomerName = new StringOperationFilterInput { StartsWith = "A" } }
                },
                order: [new OrderSortInput { OrderDate = SortEnumType.Desc }])
            .Select(e => e.Nodes.Select(n => new { n.OrderId, n.OrderDate })), "Orders_FilterAndSortInputs");

    [Fact]
    public Task Animals_InterfaceSubtypes() =>
        MatchesSnapshot(safeModeClient.Query.Animals()
            .Include(e => e.Nodes.Select(n => n.Pig()))
            .Include(e => e.Nodes.Select(n => n.Spider()))
            .Select(e => e.Nodes), "Animals_InterfaceSubtypes");

    [Fact]
    public Task Customers_SelectMany() =>
        MatchesSnapshot(safeModeClient.Query.Customers()
            .Select(e => e.SelectMany(c => c.Orders, (c, o) => new { c.CustomerName, o.OrderId })),
            "Customers_SelectMany");

    [Fact]
    public Task Mutation_ScalarArgument() =>
        MatchesSnapshot(safeModeClient.Mutation.SetName("Magnus").Select(), "Mutation_ScalarArgument");

    [Fact]
    public Task Mutation_InputObject() =>
        MatchesSnapshot(safeModeClient.Mutation.AddCustomer(new CustomerInput
            {
                CustomerId = FixedId, CustomerName = "New Customer", Orders = [], Status = CustomerStatus.Active
            })
            .Select(e => e.CustomerId), "Mutation_InputObject");

    [Fact]
    public Task SafeModeOff_IncludesEveryPrimitive() =>
        MatchesSnapshot(client.Query.Orders(1)
            .Include(e => e.Nodes.Select(n => n.Customer))
            .Select(e => e.Nodes), "SafeModeOff_IncludesEveryPrimitive");

    [Fact]
    public Task NullableClient_ListOfNullables() =>
        MatchesSnapshot(nullableClient.Query.CustomerListAllNullable().Select(), "NullableClient_ListOfNullables");

    [Fact]
    public Task NullableClient_ListInList() =>
        MatchesSnapshot(nullableClient.Query.CustomerListInList().Select(), "NullableClient_ListInList");

    private static async Task MatchesSnapshot<T, TResult>(GraphQueryExecute<T, TResult> query, string name)
    {
        Snapshot.Match(await query.GetRequestAsJsonAsync(), "Request." + name);
    }
}
