using Linq2GraphQL.Client.AI;
using Linq2GraphQL.Client.AI.Schema;
using Linq2GraphQL.TestClient;
using Shouldly;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Covers what the agent is shown of the scoped schema: the three detail levels as snapshots of the test
/// schema, and the collapsing of connections, filters and sort inputs.
/// </summary>
public class SchemaRendererTests
{
    private static ScopedSchema Typical() => ScopedSchema.Create<SampleClient>(SampleClient.Sdl, o =>
    {
        o.Instructions("All dates are UTC.");
        o.AllowQuery(c => c.Query.Hello).Describe("Greets someone.\nUse it to test the connection.");
        o.AllowQuery(c => c.Query.Customers).Describe("All customers.");
        o.AllowQuery(c => c.Query.Orders).Argument("first", "Max 50.", append: true);
        o.AllowQuery(c => c.Query.Animals);
        o.Describe<Customer>("A B2B customer \"account\".");
        o.Describe(CustomerStatus.Disabled, "Closed account.");
        o.Omit<Customer>(c => c.Address);
    });

    [Theory]
    [InlineData(nameof(Detail.Index))]
    [InlineData(nameof(Detail.Outline))]
    [InlineData(nameof(Detail.Full))]
    public void Typical_Snapshot(string detail)
    {
        Snapshot.Match(SchemaRenderer.Render(Typical(), Enum.Parse<Detail>(detail)), $"Renderer.Typical.{detail}");
    }

    [Fact]
    public void AllQueries_Index_Snapshot()
    {
        var schema = ScopedSchema.Create<SampleClient>(SampleClient.Sdl, o => o.AllowAllQueries());

        Snapshot.Match(SchemaRenderer.Render(schema, Detail.Index), "Renderer.AllQueries.Index");
    }

    [Fact]
    public void Connections_AreCollapsedToPagedNodeLists()
    {
        var schema = ScopedSchema.Create<SampleClient>(SampleClient.Sdl, o =>
        {
            o.AllowQuery(c => c.Query.Orders);
            o.AllowQuery(c => c.Query.OrdersOffsetPaging);
        });

        var index = SchemaRenderer.Render(schema, Detail.Index);

        index.ShouldContain("orders(first, after, last, before, where, order): [Order] @paged(totalCount)");
        index.ShouldContain("ordersOffsetPaging(first, after, last, before, where, order): [Order] @paged\n");
        index.ShouldContain("@paged fields return a page");
        index.ShouldNotContain("OrdersConnection");
        index.ShouldNotContain("PageInfo,");
        index.ShouldNotContain("OrdersEdge");
    }

    [Fact]
    public void Filters_AreSummarisedPerEntity()
    {
        var schema = ScopedSchema.Create<SampleClient>(SampleClient.Sdl, o => o.AllowQuery(c => c.Query.Orders));

        var outline = SchemaRenderer.Render(schema, Detail.Outline);

        outline.ShouldContain("`where` filters:");
        outline.ShouldContain("- Operator sets: text = eq neq contains ncontains in nin startsWith nstartsWith endsWith nendsWith; " +
                              "comparable = eq neq in nin gt ngt gte ngte lt nlt lte nlte; equality = eq neq in nin.");
        outline.ShouldContain("OrderFilterInput: orderId (comparable)");
        outline.ShouldContain("customer (CustomerFilterInput)");
        outline.ShouldContain("lines (list of OrderLineFilterInput: some/all/none, any)");
        outline.ShouldContain("data (list of comparable: some/all/none, any)");
        outline.ShouldContain("OrderSortInput: orderId, customer (CustomerSortInput), address (AddressSortInput)");
        outline.ShouldNotContain("input StringOperationFilterInput");
        outline.ShouldNotContain("enum SortEnumType");
    }

    [Fact]
    public void TypesAreGroupedByKind()
    {
        var index = SchemaRenderer.Render(Typical(), Detail.Index);

        index.ShouldContain("Types: IAnimal, Pig, Spider, Order, Customer, OrderLine, Address, Item, CustomerStatus, AddressType, UUID,");
    }

    [Fact]
    public void DeprecatedFilterFields_AreLeftOut()
    {
        var schema = ScopedSchema.Create<SampleClient>(SampleClient.Sdl, o => o.AllowQuery(c => c.Query.Orders));

        SchemaRenderer.Render(schema, Detail.Outline).ShouldNotContain("grade");
    }

    [Fact]
    public void Conventions_OnlyAppearWhenUsed()
    {
        var schema = ScopedSchema.Create<SampleClient>(SampleClient.Sdl, o => o.AllowQuery(c => c.Query.Hello));

        var full = SchemaRenderer.Render(schema, Detail.Full);

        full.ShouldNotContain("Conventions:");
        full.ShouldStartWith("Queries:\n  hello(name: String = \"World\"): String");
    }

    [Fact]
    public void RenderTypes_SkipsPlumbingAndUnknownNames()
    {
        var text = SchemaRenderer.RenderTypes(Typical(), ["Customer", "OrdersConnection", "Nope", "Customer"]);

        text.ShouldStartWith("\"A B2B customer \\\"account\\\".\"\ntype Customer {");
        text.ShouldNotContain("OrdersConnection");
        text.Split("type Customer").Length.ShouldBe(2);
    }
}
