using System.Text.Json;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.AI;
using Linq2GraphQL.TestClient;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Covers AsAgentTools: the four AIFunctions as an agent framework calls them, the per-user access token,
/// query logging and typed examples, against the in-process test server.
/// </summary>
public class AgentToolsExtensionsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> application = new();
    private readonly List<string?> authorizations = [];
    private readonly SampleClient client;

    public AgentToolsExtensionsTests()
    {
        var recorder = new RecordingHandler(authorizations) { InnerHandler = application.Server.CreateHandler() };
        var httpClient = new HttpClient(recorder) { BaseAddress = new Uri("https://localhost:7184/graphql/") };
        client = new SampleClient(httpClient, Options.Create(new GraphClientOptions()),
            new ServiceCollection().AddMemoryCache().BuildServiceProvider());
    }

    public void Dispose() => application.Dispose();

    private static AIFunction Tool(IList<AIFunction> tools, string name) => tools.Single(e => e.Name == name);

    private static async Task<string> InvokeAsync(AIFunction tool, Dictionary<string, object?> arguments)
    {
        var result = await tool.InvokeAsync(new AIFunctionArguments(arguments));
        return result is JsonElement { ValueKind: JsonValueKind.String } text ? text.GetString()! : result?.ToString() ?? "";
    }

    [Fact]
    public void AsAgentTools_ReturnsTheFourTools()
    {
        var tools = client.AsAgentTools(o => o.AllowQuery(c => c.Query.Customers).Describe("All customers."));

        tools.Select(e => e.Name).ShouldBe(["graphql_query", "describe_operation", "describe_type", "search_schema"]);
        Tool(tools, "graphql_query").Description.ShouldContain("customers: [Customer]  – All customers.");
        Tool(tools, "graphql_query").JsonSchema.GetRawText().ShouldContain("\"query\"");
    }

    [Fact]
    public async Task GraphqlQuery_RunsTheQuery()
    {
        var tools = client.AsAgentTools(o => o.AllowQuery(c => c.Query.Hello));

        var result = await InvokeAsync(Tool(tools, "graphql_query"), new()
        {
            ["query"] = "query($n: String) { hello(name: $n) }",
            ["variables"] = JsonDocument.Parse("""{ "n": "Agent" }""").RootElement
        });

        result.ShouldContain("Hello, Agent!");
    }

    [Fact]
    public async Task DescribeAndSearchTools_Answer()
    {
        var tools = client.AsAgentTools(o => o.AllowQuery(c => c.Query.Customers));

        (await InvokeAsync(Tool(tools, "describe_operation"), new() { ["names"] = new[] { "customers" } }))
            .ShouldContain("Starter query:");
        (await InvokeAsync(Tool(tools, "describe_type"), new() { ["names"] = new[] { "Customer" } }))
            .ShouldContain("type Customer {");
        (await InvokeAsync(Tool(tools, "search_schema"), new() { ["text"] = "customerName" }))
            .ShouldContain("field Customer.customerName");
    }

    [Fact]
    public async Task AccessToken_IsSentWithEveryQuery()
    {
        var tools = client.AsAgentTools(o =>
        {
            o.AllowQuery(c => c.Query.Hello);
            o.AccessToken = _ => ValueTask.FromResult("user-token");
        });

        await InvokeAsync(Tool(tools, "graphql_query"), new() { ["query"] = "{ hello }" });

        authorizations.ShouldBe(["Bearer user-token"]);
    }

    [Fact]
    public async Task OnQuery_SeesSentAndRejectedQueries()
    {
        var logs = new List<AgentQueryLog>();
        var tools = client.AsAgentTools(o =>
        {
            o.AllowQuery(c => c.Query.Hello);
            o.OnQuery(logs.Add);
            o.OnQuery(_ => throw new InvalidOperationException("A broken logger must not break the agent."));
        });

        var sent = await InvokeAsync(Tool(tools, "graphql_query"), new() { ["query"] = "{ hello }" });
        await InvokeAsync(Tool(tools, "graphql_query"), new() { ["query"] = "{ customers { customerName } }" });

        sent.ShouldContain("Hello, World!");
        logs.Count.ShouldBe(2);
        logs[0].WasSent.ShouldBeTrue();
        logs[0].Query.ShouldBe("{ hello }");
        logs[1].WasSent.ShouldBeFalse();
        logs[1].ValidationErrors.ShouldContain(e => e.Contains("Query customers is not available"));
        authorizations.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Examples_AreShownToTheAgent()
    {
        var tools = client.AsAgentTools(o =>
        {
            o.AllowQuery(c => c.Query.Customers);
            o.AllowQuery(c => c.Query.Hello);
            o.Example("Customer names", "The name and status of every customer",
                c => c.Query.Customers().Select(e => e.Select(x => new { x.CustomerName, x.Status })));
            o.Example("Greeting", "Greets a named person", c => c.Query.Hello("Jocke").Select());
        });

        var index = Tool(tools, "graphql_query").Description!;
        index.ShouldContain("Examples (describe_operation shows their queries):");
        index.ShouldContain("Customer names  – The name and status of every customer");

        var customers = await InvokeAsync(Tool(tools, "describe_operation"), new() { ["names"] = new[] { "customers" } });
        customers.ShouldContain("Customer names  – The name and status of every customer");
        customers.ShouldContain("customerName");
        customers.ShouldNotContain("Greeting");

        var hello = await InvokeAsync(Tool(tools, "describe_operation"), new() { ["names"] = new[] { "hello" } });
        hello.ShouldContain("Greeting  – Greets a named person");
        hello.ShouldContain("variables: {");
        hello.ShouldContain("\"Jocke\"");

        (await InvokeAsync(Tool(tools, "search_schema"), new() { ["text"] = "greeting" }))
            .ShouldStartWith("example Greeting");
    }

    [Fact]
    public void Example_OutsideTheScope_FailsAtSetup()
    {
        var ex = Should.Throw<AgentConfigurationException>(() => client.AsAgentTools(o =>
        {
            o.AllowQuery(c => c.Query.Customers);
            o.Omit<Customer>(c => c.Status);
            o.Example("Statuses", "Every customer's status",
                c => c.Query.Customers().Select(e => e.Select(x => new { x.CustomerName, x.Status })));
        }));

        ex.Message.ShouldContain("Example \"Statuses\"");
        ex.Message.ShouldContain("Customer has no field status");
    }

    [Fact]
    public async Task ExplicitSdl_Works()
    {
        var tools = client.AsAgentTools("type Query { hello(name: String): String }", o => o.AllowAllQueries());

        (await InvokeAsync(Tool(tools, "graphql_query"), new() { ["query"] = "{ hello }" })).ShouldContain("Hello, World!");
    }

    private sealed class RecordingHandler(List<string?> authorizations) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            authorizations.Add(request.Headers.Authorization?.ToString());
            return base.SendAsync(request, cancellationToken);
        }
    }
}
