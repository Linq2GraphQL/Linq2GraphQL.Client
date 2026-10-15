using System.Runtime.CompilerServices;
using System.Text.Json;
using Linq2GraphQL.Client.AI;
using Linq2GraphQL.TestClient;
using Microsoft.Extensions.AI;
using Shouldly;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Runs the whole agent loop in CI without a model: a scripted chat client plays the model's part, asking for
/// tool calls in a fixed order, and Microsoft.Extensions.AI's function invocation runs the real tools against
/// the test server. It checks the wiring end to end, not answer quality; that is what the sample's eval mode
/// does with a real model.
/// </summary>
public class AgentLoopTests : IClassFixture<SampleClientFixture>
{
    private readonly SampleClient client;

    public AgentLoopTests(SampleClientFixture fixture)
    {
        client = fixture.sampleClient;
    }

    private IList<AIFunction> Tools() => client.AsAgentTools(o =>
    {
        o.AllowQuery(c => c.Query.Customers);
        o.AllowQuery(c => c.Query.Animals);
    });

    [Fact]
    public async Task ScriptedAgent_ExploresQueriesAndAnswers()
    {
        var model = new ScriptedChatClient(
            Call("search_schema", new { text = "customer" }),
            Call("describe_operation", new { names = new[] { "customers" } }),
            Call("graphql_query", new { query = "{ customers { customerName status } }" }),
            Answer());

        var response = await Agent(model).GetResponseAsync("Which customers are active?", new ChatOptions { Tools = [..Tools()] });

        model.ToolResults.Count.ShouldBe(3);
        model.ToolResults[0].ShouldContain("operation customers");
        model.ToolResults[1].ShouldContain("Starter query:");
        response.Text.ShouldContain("Universal Ltd");
        response.Text.ShouldContain("ACTIVE");
    }

    [Fact]
    public async Task ScriptedAgent_FixesARejectedQuery()
    {
        var model = new ScriptedChatClient(
            Call("graphql_query", new { query = "{ animals { name } }" }),
            Call("graphql_query", new { query = "{ animals { nodes { name } } }" }),
            Answer());

        var response = await Agent(model).GetResponseAsync("Name the animals.", new ChatOptions { Tools = [..Tools()] });

        model.ToolResults[0].ShouldContain("validationErrors");
        model.ToolResults[0].ShouldContain("This field is paged");
        response.Text.ShouldContain("Kalle");
        response.Text.ShouldContain("Imse");
    }

    private static IChatClient Agent(IChatClient model) => model.AsBuilder().UseFunctionInvocation().Build();

    private static Func<int, ChatResponse> Call(string tool, object arguments) => step =>
        new ChatResponse(new ChatMessage(ChatRole.Assistant,
        [
            new FunctionCallContent($"call-{step}", tool,
                JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(arguments)))
        ]));

    /// <summary>
    /// The final turn: answers with the last tool result, so the test can see the data flowed back.
    /// </summary>
    private static Func<int, ChatResponse> Answer() => _ => null!;

    private sealed class ScriptedChatClient(params Func<int, ChatResponse>[] script) : IChatClient
    {
        private int step;

        public List<string> ToolResults { get; } = [];

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var results = messages.SelectMany(e => e.Contents).OfType<FunctionResultContent>()
                .Select(e => e.Result is JsonElement { ValueKind: JsonValueKind.String } text ? text.GetString()! : e.Result?.ToString() ?? "")
                .ToList();
            ToolResults.Clear();
            ToolResults.AddRange(results);

            var response = script[step](step);
            step++;
            return Task.FromResult(response ?? new ChatResponse(new ChatMessage(ChatRole.Assistant, ToolResults.LastOrDefault() ?? "")));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var update in (await GetResponseAsync(messages, options, cancellationToken)).ToChatResponseUpdates())
            {
                yield return update;
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
