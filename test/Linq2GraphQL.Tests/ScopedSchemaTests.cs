using Linq2GraphQL.Client.AI;
using Linq2GraphQL.Client.AI.Schema;
using Linq2GraphQL.TestClient;
using Shouldly;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Covers building the agent's scoped schema from the test client's SDL: allowed queries, reachable types,
/// omitted and deprecated members, descriptions, and configuration errors.
/// </summary>
public class ScopedSchemaTests
{
    private static ScopedSchema Scope(Action<AgentOptions<SampleClient>> configure) =>
        ScopedSchema.Create(SampleClient.Sdl, configure);

    private static string[] Fields(ScopedSchema schema, string type) =>
        schema.Schema.Type(type).Fields.Select(e => e.Name).ToArray();

    [Fact]
    public void AllowQuery_KeepsOnlyThatRootAndWhatItReaches()
    {
        var schema = Scope(o => o.AllowQuery(c => c.Query.Customers));

        Fields(schema, "Query").ShouldBe(["customers"]);
        schema.Schema.Types.Keys.ShouldContain("Customer");
        schema.Schema.Types.Keys.ShouldContain("Order");
        schema.Schema.Types.Keys.ShouldContain("CustomerStatus");
        schema.Schema.Types.Keys.ShouldNotContain("Pig");
        schema.Schema.Types.Keys.ShouldNotContain("OrdersConnection");
        schema.Schema.Types.Keys.ShouldNotContain("Mutation");
        schema.Schema.Types.Keys.ShouldNotContain("Subscription");
    }

    [Fact]
    public void AllowQuery_InterfaceRoot_ReachesItsImplementations()
    {
        var schema = Scope(o => o.AllowQuery(c => c.Query.Animals));

        schema.Schema.Types.Keys.ShouldContain("IAnimal");
        schema.Schema.Types.Keys.ShouldContain("Pig");
        schema.Schema.Types.Keys.ShouldContain("Spider");
        schema.Schema.Type("IAnimal").PossibleTypes.ShouldBe(["Pig", "Spider"], ignoreOrder: true);
    }

    [Fact]
    public void AllowAllQueries_Except_LeavesOutTheExcludedRoots()
    {
        var schema = Scope(o => o.AllowAllQueries().Except(c => c.Query.RaiseError, c => c.Query.RaiseAuthError));

        Fields(schema, "Query").ShouldContain("hello");
        Fields(schema, "Query").ShouldContain("animals");
        Fields(schema, "Query").ShouldNotContain("raiseError");
        Fields(schema, "Query").ShouldNotContain("raiseAuthError");
    }

    [Fact]
    public void RootFields_FollowSchemaOrder()
    {
        var schema = Scope(o =>
        {
            o.AllowQuery(c => c.Query.Customers);
            o.AllowQuery(c => c.Query.Hello);
        });

        Fields(schema, "Query").ShouldBe(["hello", "customers"]);
    }

    [Fact]
    public void Omit_RemovesTheFieldAndTypesOnlyItReached()
    {
        var schema = Scope(o =>
        {
            o.AllowQuery(c => c.Query.Customers);
            o.Omit<Customer>(c => c.Orders);
        });

        Fields(schema, "Customer").ShouldNotContain("orders");
        schema.Schema.Types.Keys.ShouldNotContain("Order");
    }

    [Fact]
    public void Omit_AlsoRemovesTheFieldFromFilterAndSortInputs()
    {
        var schema = Scope(o =>
        {
            o.AllowQuery(c => c.Query.Orders);
            o.Omit<Customer>(c => c.Address);
        });

        Fields(schema, "Customer").ShouldNotContain("address");
        Fields(schema, "CustomerFilterInput").ShouldNotContain("address");
        Fields(schema, "CustomerSortInput").ShouldNotContain("address");
        Fields(schema, "CustomerFilterInput").ShouldContain("customerName");
    }

    [Fact]
    public void Omit_FieldWithArguments_ResolvesWithoutAnAttribute()
    {
        var schema = Scope(o =>
        {
            o.AllowQuery(c => c.Query.Customers);
            o.Omit<Customer>(c => c.RelatedCustomers);
        });

        Fields(schema, "Customer").ShouldNotContain("relatedCustomers");
        Fields(schema, "Customer").ShouldContain("relatedCustomer");
    }

    [Fact]
    public void DeprecatedEnumValues_AreNotInScope()
    {
        var schema = Scope(o => o.AllowQuery(c => c.Query.Customers));

        schema.Schema.Type("CustomerStatus").EnumValues.Select(e => e.Name).ShouldBe(["ACTIVE", "DISABLED"]);
    }

    [Fact]
    public void Descriptions_ReplaceOrAppend()
    {
        var schema = Scope(o =>
        {
            o.AllowQuery(c => c.Query.Customers).Describe("Search customers.");
            o.AllowQuery(c => c.Query.Orders)
                .Argument("first", "Max 50.", append: true);
            o.Describe<Customer>("A B2B customer account.");
            o.Describe<Customer>(c => c.Status, "ACTIVE = currently invoiced.");
            o.Describe(CustomerStatus.Disabled, "Closed account.");
        });

        schema.QueryType.Field("customers").Description.ShouldBe("Search customers.");
        schema.QueryType.Field("orders").Arguments.Single(e => e.Name == "first").Description
            .ShouldBe("Returns the first _n_ elements from the list.\nMax 50.");
        schema.Schema.Type("Customer").Description.ShouldBe("A B2B customer account.");
        schema.Schema.Type("Customer").Field("status").Description.ShouldBe("ACTIVE = currently invoiced.");
        schema.Schema.Type("CustomerStatus").EnumValues.Single(e => e.Name == "DISABLED").Description
            .ShouldBe("Closed account.");
    }

    [Fact]
    public void Instructions_AreKept()
    {
        var schema = Scope(o =>
        {
            o.AllowQuery(c => c.Query.Hello);
            o.Instructions("All dates are UTC.");
        });

        schema.Instructions.ShouldBe(["All dates are UTC."]);
    }

    [Fact]
    public void Mutation_IsRejectedForNow()
    {
        var ex = Should.Throw<AgentConfigurationException>(() =>
            Scope(o => o.AllowQuery(c => c.Mutation.SetName)));

        ex.Message.ShouldContain("SetName is a mutation");
    }

    [Fact]
    public void NothingAllowed_Fails()
    {
        var ex = Should.Throw<AgentConfigurationException>(() => Scope(o => o.Instructions("Hi")));

        ex.Message.ShouldContain("No queries are allowed");
    }

    [Fact]
    public void ConfigurationErrors_AreReportedTogether()
    {
        var ex = Should.Throw<AgentConfigurationException>(() => Scope(o =>
        {
            o.AllowQuery(c => c.Query.Orders).Argument("pageSize", "Max 50.");
            o.Omit<Customer>(c => c.Address);
            o.Describe<Customer>(c => c.Address, "Where they live.");
#pragma warning disable CS0618 // Describing the deprecated value is the point of the test.
            o.Describe(CustomerStatus.InTransit, "Moving.");
#pragma warning restore CS0618
        }));

        ex.Message.ShouldContain("Query orders has no argument pageSize");
        ex.Message.ShouldContain("Customer.Address is described but also omitted");
        ex.Message.ShouldContain("CustomerStatus.IN_TRANSIT is described but deprecated");
    }

    [Fact]
    public void NonOperationExpression_FailsWithClearMessage()
    {
        // c => c.Query does not compile; a lambda that is not an operation does, and is rejected at setup.
        var ex = Should.Throw<ArgumentException>(() => Scope(o => o.AllowQuery(c => (Func<string>)(() => "x"))));

        ex.Message.ShouldContain("c => c.Query.Customers");
    }

    [Fact]
    public void Sdl_WithSchemaBlockAndExtensions_IsRead()
    {
        const string sdl = """
            schema { query: Root }
            type Root { hello: String }
            extend type Root { customers: [Customer] }
            type Customer { name: String }
            """;

        var schema = ScopedSchema.Create<SampleClient>(sdl, o => o.AllowQuery(c => c.Query.Customers));

        schema.Schema.QueryType.ShouldBe("Root");
        Fields(schema, "Root").ShouldBe(["customers"]);
    }

    [Fact]
    public void Sdl_UnknownType_FailsWithClearMessage()
    {
        var ex = Should.Throw<AgentConfigurationException>(() =>
            ScopedSchema.Create<SampleClient>("type Query { thing: Thing }", o => o.AllowAllQueries()));

        ex.Message.ShouldContain("Thing");
    }
}
