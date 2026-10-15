using System.Diagnostics;
using System.Text.Json;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.AI;
using Linq2GraphQL.TestClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.Agent.Sample;

/// <summary>
/// A small agent over the test GraphQL server, built with AsAgentTools.
///   chat                 talk to the agent (the default)
///   eval [file]          run the questions in questions.json and report answer rate, tool calls and tokens
/// The test server runs in-process unless Agent:ServerUrl points at a running one.
/// </summary>
public static class AgentSampleApp
{
    private const string SystemPrompt =
        "You answer questions about the data behind a GraphQL API, using the tools. If you are unsure which " +
        "operation or field to use, call describe_operation, describe_type or search_schema before querying. " +
        "If a query is rejected, fix every error listed and try again. Answer briefly, with the values you found.";

    public static async Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets(typeof(AgentSampleApp).Assembly, optional: true)
            .AddEnvironmentVariables("LINQ2GRAPHQL_")
            .Build();

        var settings = AgentSettings.From(configuration, out var problem);
        if (settings == null)
        {
            Console.Error.WriteLine(problem);
            Console.Error.WriteLine("""

                Set the model to use, for example with user secrets (stored outside the repository):

                  dotnet user-secrets --project test/Linq2GraphQL.Agent.Sample set Agent:Endpoint "https://<resource>.cognitiveservices.azure.com/"
                  dotnet user-secrets --project test/Linq2GraphQL.Agent.Sample set Agent:Deployment "<deployment>"

                Azure AI Foundry signs in with your own account (az login or Visual Studio); no key is needed.
                For another OpenAI-compatible endpoint, such as Ollama, also set Agent:Provider to
                OpenAICompatible and, if the endpoint needs one, Agent:ApiKey.
                Environment variables work too: LINQ2GRAPHQL_Agent__Endpoint and so on.
                """);
            return 1;
        }

        using var server = StartServer(configuration["Agent:ServerUrl"], out var httpClient);
        var client = new SampleClient(httpClient, Options.Create(new GraphClientOptions()),
            new ServiceCollection().AddMemoryCache().BuildServiceProvider());
        var chat = ChatClientFactory.Create(settings);
        var verbose = args.Contains("--verbose");

        var mode = args.FirstOrDefault(e => !e.StartsWith("--")) ?? "chat";
        return mode switch
        {
            "chat" => await ChatAsync(chat, client, verbose),
            "eval" => await EvalAsync(chat, client, args.Skip(1).FirstOrDefault(e => !e.StartsWith("--")) ?? "questions.json", verbose),
            _ => Usage()
        };
    }

    /// <summary>
    /// The agent's scope over the test schema. Everything the agent sees comes from here.
    /// </summary>
    internal static IList<AIFunction> Tools(SampleClient client, Action<AgentQueryLog>? log) => client.AsAgentTools(o =>
    {
        o.Instructions("Dates are UTC.");
        o.AllowQuery(c => c.Query.Hello).Describe("Greets someone by name; a quick way to test the API.");
        o.AllowQuery(c => c.Query.Customers).Describe("All customers with their orders.");
        o.AllowQuery(c => c.Query.Orders).Describe("Orders, paged and filterable.");
        o.AllowQuery(c => c.Query.Animals).Describe("The animals: pigs and spiders.");
        o.Describe<Customer>("A customer account.");
        o.Example("Customer statuses", "Every customer's name and status",
            c => c.Query.Customers().Select(e => e.Select(x => new { x.CustomerName, x.Status })));
        if (log != null)
        {
            o.OnQuery(log);
        }
    });

    private static async Task<int> ChatAsync(IChatClient chat, SampleClient client, bool verbose)
    {
        var tools = Tools(client, verbose ? PrintQuery : null);
        var options = new ChatOptions { Tools = [..tools] };
        var history = new List<ChatMessage> { new(ChatRole.System, SystemPrompt) };

        Console.WriteLine("Ask about customers, orders or animals. An empty line ends the chat.");
        while (true)
        {
            Console.Write("\n> ");
            var question = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(question))
            {
                return 0;
            }

            history.Add(new ChatMessage(ChatRole.User, question));
            var response = await chat.GetResponseAsync(history, options);
            history.AddMessages(response);
            Console.WriteLine(response.Text);
        }
    }

    private static async Task<int> EvalAsync(IChatClient chat, SampleClient client, string file, bool verbose)
    {
        var path = Path.IsPathRooted(file) ? file : Path.Combine(AppContext.BaseDirectory, file);
        var questions = JsonSerializer.Deserialize<List<EvalQuestion>>(await File.ReadAllTextAsync(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        var results = new List<EvalResult>();
        foreach (var question in questions)
        {
            var queries = new List<AgentQueryLog>();
            var tools = Tools(client, log =>
            {
                queries.Add(log);
                if (verbose)
                {
                    PrintQuery(log);
                }
            });

            var watch = Stopwatch.StartNew();
            var response = await chat.GetResponseAsync(
                [new ChatMessage(ChatRole.System, SystemPrompt), new ChatMessage(ChatRole.User, question.Question)],
                new ChatOptions { Tools = [..tools] });

            var answer = response.Text;
            var result = new EvalResult(
                question.Question,
                question.Expected.All(e => answer.Contains(e, StringComparison.OrdinalIgnoreCase)),
                response.Messages.SelectMany(e => e.Contents).OfType<FunctionCallContent>().Count(),
                queries.Count(e => !e.WasSent),
                response.Usage?.TotalTokenCount ?? 0,
                watch.Elapsed,
                answer);
            results.Add(result);

            Console.WriteLine($"{(result.Correct ? "PASS" : "FAIL")}  tools {result.ToolCalls,2}  rejected {result.RejectedQueries}  " +
                              $"tokens {result.Tokens,6}  {result.Duration.TotalSeconds,5:0.0}s  {result.Question}");
            if (!result.Correct)
            {
                Console.WriteLine($"      expected: {string.Join(", ", question.Expected)}");
                Console.WriteLine($"      answer:   {answer.ReplaceLineEndings(" ")}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Answered {results.Count(e => e.Correct)} of {results.Count} correctly; " +
                          $"{results.Average(e => e.ToolCalls):0.0} tool calls, " +
                          $"{results.Average(e => e.Tokens):0} tokens and " +
                          $"{results.Average(e => e.Duration.TotalSeconds):0.0}s per question on average.");

        var report = Path.Combine(AppContext.BaseDirectory, $"eval-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
        await File.WriteAllTextAsync(report, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Details: {report}");

        return results.All(e => e.Correct) ? 0 : 2;
    }

    private static IDisposable StartServer(string? serverUrl, out HttpClient httpClient)
    {
        if (!string.IsNullOrWhiteSpace(serverUrl))
        {
            httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
            return httpClient;
        }

        // The factory looks for the server's content root next to the solution file, which only works from a
        // test project; the test server reads nothing from it, so the sample's own output folder will do.
        var application = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseContentRoot(AppContext.BaseDirectory));
        httpClient = application.CreateClient(new() { BaseAddress = new Uri("https://localhost/graphql/") });
        return application;
    }

    private static void PrintQuery(AgentQueryLog log)
    {
        var color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  [{(log.WasSent ? "sent" : "rejected")}] {log.Query.ReplaceLineEndings(" ")}");
        foreach (var error in log.ValidationErrors.Concat(log.Errors))
        {
            Console.WriteLine($"    {error}");
        }

        Console.ForegroundColor = color;
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: Linq2GraphQL.Agent.Sample [chat | eval [questions.json]] [--verbose]");
        return 1;
    }

    private sealed record EvalQuestion(string Question, string[] Expected);

    private sealed record EvalResult(string Question, bool Correct, int ToolCalls, int RejectedQueries, long Tokens,
        TimeSpan Duration, string Answer);
}
