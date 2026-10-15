using System.Text.Json;
using Linq2GraphQL.Client.AI.Schema;
using Linq2GraphQL.TestClient;
using Shouldly;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Covers the validator that checks agent-written queries against the scoped schema before they are sent.
/// The scope allows hello, customers, orders and animals, and omits Customer.address.
/// </summary>
public class QueryValidatorTests
{
    private static readonly QueryValidator Validator = new(ScopedSchema.Create<SampleClient>(SampleClient.Sdl, o =>
    {
        o.AllowQuery(c => c.Query.Hello);
        o.AllowQuery(c => c.Query.Customers);
        o.AllowQuery(c => c.Query.Orders);
        o.AllowQuery(c => c.Query.Animals);
        o.Omit<Customer>(c => c.Address);
    }));

    private static ValidationResult Validate(string query, string? variables = null) =>
        Validator.Validate(query, variables == null ? null : JsonDocument.Parse(variables).RootElement);

    private static void ShouldBeValid(string query, string? variables = null)
    {
        var result = Validate(query, variables);
        result.IsValid.ShouldBeTrue(result.ToString());
    }

    private static string ShouldFail(string query, string? variables = null)
    {
        var result = Validate(query, variables);
        result.IsValid.ShouldBeFalse();
        return result.ToString();
    }

    /// <summary>
    /// The validator must not accept what the server rejects: every query it passes runs without errors.
    /// </summary>
    [Theory]
    [InlineData("{ hello }")]
    [InlineData("query { hello(name: \"Jocke\") }")]
    [InlineData("{ customers { customerName status orders { orderId lines { quantity } } } }")]
    [InlineData("{ c: customers { name: customerName } __typename }")]
    [InlineData("query { customers { ...C } } fragment C on Customer { customerName relatedCustomer(relationType: 1) { customerId } }")]
    [InlineData("{ animals(first: 5) { nodes { name ... on Pig { spices } ... on Spider { poisonous } } pageInfo { hasNextPage endCursor } totalCount } }")]
    [InlineData("{ orders(where: { customer: { status: { eq: ACTIVE } }, or: [{ orderDate: { gt: \"2024-01-01T00:00:00Z\" } }] }, order: [{ orderDate: DESC }]) { nodes { orderId } } }")]
    [InlineData("{ orders(where: { lines: { some: { quantity: { gte: 2 } } } }) { edges { cursor node { orderId } } } }")]
    [InlineData("{ customers { customerName @include(if: true) } }")]
    public async Task ValidQueries_AreAcceptedByTheServer(string query)
    {
        ShouldBeValid(query);

        using var fixture = new SampleClientFixture();
        var result = await fixture.sampleClient.GraphClient.ExecuteAsync(query);

        result.HasErrors.ShouldBeFalse(string.Join("; ", result.Errors?.Select(e => e.Message) ?? []));
    }

    [Fact]
    public void FilterInVariables_Passes()
    {
        ShouldBeValid("query($w: OrderFilterInput, $n: Int) { orders(where: $w, first: $n) { nodes { orderId } } }",
            """{ "w": { "customer": { "customerName": { "contains": "A" }, "status": { "in": ["ACTIVE"] } } }, "n": 10 }""");
    }

    [Fact]
    public void Mutation_IsRejected()
    {
        ShouldFail("mutation { setName(name: \"x\") }").ShouldContain("Only queries are allowed; this is a mutation.");
    }

    [Fact]
    public void RootNotAllowed_ListsTheAvailableQueries()
    {
        ShouldFail("{ raiseError }").ShouldContain("Query raiseError is not available. Available: hello, customers, orders, animals.");
    }

    [Fact]
    public void Typo_SuggestsTheClosestName()
    {
        ShouldFail("{ custmers { customerName } }").ShouldContain("Did you mean `customers`?");
        ShouldFail("{ customers { custmerName } }").ShouldContain("Customer has no field custmerName. Did you mean `customerName`?");
    }

    [Fact]
    public void OmittedField_CannotBeSelected()
    {
        var errors = ShouldFail("{ customers { address { name } } }");

        errors.ShouldContain("customers.address: Customer has no field address.");
        errors.ShouldNotContain("Available: customerId, customerName, status, orders, address");
    }

    [Fact]
    public void OmittedField_CannotBeFilteredOn()
    {
        ShouldFail("{ orders(where: { customer: { address: { name: { eq: \"x\" } } } }) { nodes { orderId } } }")
            .ShouldContain("orders(where).customer.address: CustomerFilterInput has no field address.");
    }

    [Fact]
    public void OmittedField_CannotBeFilteredOnThroughVariables()
    {
        ShouldFail("query($w: OrderFilterInput) { orders(where: $w) { nodes { orderId } } }",
                """{ "w": { "customer": { "address": { "name": { "eq": "x" } } } } }""")
            .ShouldContain("$w.customer.address: CustomerFilterInput has no field address.");
    }

    [Fact]
    public void OmittedField_CannotBeSortedOn()
    {
        ShouldFail("{ orders(order: [{ customer: { address: { name: ASC } } }]) { nodes { orderId } } }")
            .ShouldContain("CustomerSortInput has no field address.");
    }

    [Fact]
    public void PagedField_SelectedDirectly_GetsTheFix()
    {
        ShouldFail("{ orders { orderId } }")
            .ShouldContain("orders.orderId: This field is paged; select orderId inside `nodes { orderId }`");
    }

    [Fact]
    public void LeafAndCompositeSelections_AreChecked()
    {
        ShouldFail("{ hello { length } }").ShouldContain("hello is a String and has no fields to select.");
        ShouldFail("{ customers }").ShouldContain("customers returns Customer; select fields, for example `customers { customerId customerName status }`.");
        ShouldFail("{ orders }").ShouldContain("orders returns a page of Order; select fields, for example `orders { nodes { orderId orderDate orderDateTime } pageInfo { hasNextPage endCursor } }`.");
    }

    [Fact]
    public void EnumValues_AreChecked()
    {
        ShouldFail("{ orders(where: { customer: { status: { eq: \"ACTIVE\" } } }) { nodes { orderId } } }")
            .ShouldContain("CustomerStatus values are written without quotes");
        ShouldFail("{ orders(where: { customer: { status: { eq: ACTIV } } }) { nodes { orderId } } }")
            .ShouldContain("CustomerStatus has no value ACTIV. Did you mean `ACTIVE`?");
    }

    [Fact]
    public void Introspection_IsBlocked()
    {
        ShouldFail("{ __schema { types { name } } }").ShouldContain("Introspection is not available");
        ShouldFail("{ __type(name: \"Customer\") { name } }").ShouldContain("Introspection is not available");
    }

    [Fact]
    public void Arguments_AreChecked()
    {
        ShouldFail("{ orders(pageSize: 5) { nodes { orderId } } }").ShouldContain("orders has no argument pageSize.");
        ShouldFail("{ customers { relatedCustomers { customerName } } }")
            .ShouldContain("Argument relationType (Int!) is required.");
        ShouldFail("{ orders(first: \"five\") { nodes { orderId } } }").ShouldContain("orders(first): Expected a Int value.");
    }

    [Fact]
    public void Variables_AreChecked()
    {
        ShouldFail("{ orders(where: $w) { nodes { orderId } } }").ShouldContain("Variable $w is not declared.");
        ShouldFail("query($w: String) { orders(where: $w) { nodes { orderId } } }")
            .ShouldContain("Variable $w is declared as String but OrderFilterInput is expected here.");
        ShouldFail("query($w: SecretFilterInput) { orders(where: $w) { nodes { orderId } } }")
            .ShouldContain("Type SecretFilterInput is not available.");
        ShouldFail("query($n: Int!) { orders(first: $n) { nodes { orderId } } }")
            .ShouldContain("Variable $n is required (Int!) but no value was given.");
        ShouldFail("{ hello }", """{ "extra": 1 }""").ShouldContain("Variable $extra is not declared in the query.");
    }

    [Fact]
    public void Fragments_AreChecked()
    {
        ShouldFail("{ customers { ... on Order { orderId } } }").ShouldContain("A fragment on Order cannot apply to Customer.");
        ShouldFail("{ customers { ...Missing } }").ShouldContain("Fragment Missing is not defined.");
        ShouldFail("{ customers { ...A } } fragment A on Customer { ...A }").ShouldContain("Fragment A spreads itself.");
        ShouldFail("{ animals { nodes { ... on Pig { secret } } } }").ShouldContain("Pig has no field secret.");
    }

    [Fact]
    public void Document_IsChecked()
    {
        ShouldFail("{ hello").ShouldContain("Syntax error");
        ShouldFail("query A { hello } query B { hello }").ShouldContain("Send one operation per call.");
        ShouldFail("fragment F on Customer { customerName }").ShouldContain("The document has no operation.");
        ShouldFail("{ hello @custom }").ShouldContain("Directive @custom is not supported");
    }

    [Fact]
    public void AllErrors_AreReportedAtOnce()
    {
        var result = Validate("{ custmers { x } hello { y } raiseError }");

        result.Errors.Count.ShouldBe(3);
    }
}
