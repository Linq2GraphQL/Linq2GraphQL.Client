using System.Text.Json;
using Linq2GraphQL.Client.AI.Schema;
using Linq2GraphQL.Client.AI.Tools;
using Linq2GraphQL.TestClient;
using Shouldly;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Covers the four agent tools as plain methods, against the in-process test server. The scope allows hello,
/// customers, orders and animals, and omits Customer.address.
/// </summary>
public class AgentToolsetTests : IClassFixture<SampleClientFixture>
{
    private readonly SampleClient client;
    private readonly AgentToolset tools;

    public AgentToolsetTests(SampleClientFixture fixture)
    {
        client = fixture.sampleClient;
        tools = new AgentToolset(Scope(), client.GraphClient);
    }

    private static ScopedSchema Scope() => ScopedSchema.Create<SampleClient>(SampleClient.Sdl, o =>
    {
        o.AllowQuery(c => c.Query.Hello).Describe("Greets someone.");
        o.AllowQuery(c => c.Query.Customers).Describe("All customers.");
        o.AllowQuery(c => c.Query.Orders).Argument("first", "Max 50.", append: true);
        o.AllowQuery(c => c.Query.Animals);
        o.Describe<Customer>("A B2B customer account.");
        o.Omit<Customer>(c => c.Address);
    });

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement;

    [Fact]
    public void Index_IsTheRenderedIndex()
    {
        tools.Index.ShouldBe(SchemaRenderer.Render(Scope(), Detail.Index));
    }

    [Fact]
    public async Task Query_ReturnsData()
    {
        var result = Json(await tools.QueryAsync("{ customers { customerName } }"));

        result.GetProperty("data").GetProperty("customers").GetArrayLength().ShouldBeGreaterThan(0);
        result.TryGetProperty("errors", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Query_WithVariables_ReturnsData()
    {
        var result = Json(await tools.QueryAsync("query($n: String) { hello(name: $n) }", Json("""{ "n": "Agent" }""")));

        result.GetProperty("data").GetProperty("hello").GetString().ShouldBe("Hello, Agent!");
    }

    [Fact]
    public async Task Query_Invalid_ReturnsValidationErrorsAndSendsNothing()
    {
        var sent = false;
        var watched = new AgentToolset(Scope(), client.WithRequestConfiguration((_, _) =>
        {
            sent = true;
            return ValueTask.CompletedTask;
        }).GraphClient);

        var result = Json(await watched.QueryAsync("{ customers { address { name } } }"));

        result.GetProperty("validationErrors")[0].GetString().ShouldBe("customers.address: Customer has no field address. " +
            "Available: customerId, customerName, status, orders, created, relatedCustomers, relatedCustomer.");
        result.GetProperty("note").GetString()!.ShouldContain("not sent");
        sent.ShouldBeFalse();
    }

    [Fact]
    public async Task Query_ServerError_IsReturnedAsErrors()
    {
        var tools = new AgentToolset(ScopedSchema.Create<SampleClient>(SampleClient.Sdl, o => o.AllowQuery(c => c.Query.RaiseError)),
            client.GraphClient);

        var result = Json(await tools.QueryAsync("{ raiseError(message: \"boom\") }"));

        result.GetProperty("errors")[0].GetProperty("message").GetString()!.ShouldContain("boom");
    }

    [Fact]
    public void DescribeOperations_Snapshot()
    {
        Snapshot.Match(tools.DescribeOperations(["orders", "hello", "customers"]), "Tools.DescribeOperations");
    }

    [Fact]
    public void DescribeOperations_Unknown_Suggests()
    {
        tools.DescribeOperations(["order"]).ShouldStartWith("Operation order is not available. Did you mean `orders`?");
    }

    [Fact]
    public async Task StarterQueries_AreValidAndRunOnTheServer()
    {
        var all = ScopedSchema.Create<SampleClient>(SampleClient.Sdl, o =>
            o.AllowAllQueries().Except(c => c.Query.RaiseError, c => c.Query.RaiseAuthError));
        var allTools = new AgentToolset(all, client.GraphClient);
        var validator = new QueryValidator(all);

        foreach (var operation in all.QueryType.Fields)
        {
            var query = allTools.StarterQuery(operation);
            var validation = validator.Validate(query);
            validation.IsValid.ShouldBeTrue($"{query}\n{validation}");

            var result = await client.GraphClient.ExecuteAsync(query);
            result.HasErrors.ShouldBeFalse($"{query}\n{string.Join("; ", result.Errors?.Select(e => e.Message) ?? [])}");
        }
    }

    [Fact]
    public void StarterQuery_RequiredArguments_BecomeVariables()
    {
        var schema = ScopedSchema.Create<SampleClient>("""
            type Query { customer(id: ID!, includeArchived: Boolean = false): Customer }
            type Customer { id: ID! name: String }
            """, o => o.AllowAllQueries());

        new AgentToolset(schema, client.GraphClient).StarterQuery(schema.QueryType.Field("customer"))
            .ShouldBe("query($id: ID!) { customer(id: $id) { id name } }");
    }

    [Fact]
    public void DescribeTypes_Snapshot()
    {
        Snapshot.Match(tools.DescribeTypes(["Customer", "OrderFilterInput", "OrderSortInput", "OrdersConnection", "String", "Custmer"]),
            "Tools.DescribeTypes");
    }

    [Fact]
    public void Search_RanksNamesBeforeDescriptions()
    {
        var lines = tools.Search("customer").TrimEnd().Split('\n');

        lines[0].ShouldBe("type Customer  – A B2B customer account.");
        lines.ShouldContain("operation customers: [Customer]  – All customers.");
        lines.ShouldContain("field Order.customer: Customer!");
        lines.ShouldNotContain(e => e.Contains("address"));
    }

    [Fact]
    public void Search_FindsDescriptionsAndEnumValues()
    {
        tools.Search("greets").ShouldBe("operation hello: String  – Greets someone.\n");
        tools.Search("active").ShouldContain("value CustomerStatus.ACTIVE");
    }

    [Fact]
    public void Search_NoMatch_SaysSo()
    {
        tools.Search("payment").ShouldStartWith("No operations, types or fields match \"payment\".");
    }
}
