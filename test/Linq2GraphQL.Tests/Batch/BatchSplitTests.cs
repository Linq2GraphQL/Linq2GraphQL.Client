using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.Tests.Batch;

/// <summary>
/// Batches against a scripted server that rejects requests with more root fields than it accepts, so splitting,
/// remembered sizes and failures can be driven exactly.
/// </summary>
public partial class BatchSplitTests
{
    public enum Rejection
    {
        PayloadTooLarge,
        ErrorsWithStatus400,
        ErrorsWithStatus200
    }

    [Theory]
    [InlineData(Rejection.PayloadTooLarge)]
    [InlineData(Rejection.ErrorsWithStatus400)]
    [InlineData(Rejection.ErrorsWithStatus200)]
    public async Task RejectedRequest_IsSplitUntilAccepted(Rejection rejection)
    {
        var server = new ScriptedServer { MaxRootFields = 3, Rejection = rejection };
        var client = CreateClient(server);

        var results = await ExecuteHellosAsync(client, 8);

        Assert.Equal(Enumerable.Range(0, 8).Select(i => $"hello b{i}"), results);
        Assert.Equal([8, 4, 2, 2, 4, 2, 2], server.RootFieldsPerRequest);
    }

    [Fact]
    public async Task AcceptedSize_IsRememberedForTheNextBatch()
    {
        var server = new ScriptedServer { MaxRootFields = 3 };
        var client = CreateClient(server);
        await ExecuteHellosAsync(client, 8);
        server.RootFieldsPerRequest.Clear();

        await ExecuteHellosAsync(client, 8, maxDegreeOfParallelism: 1);

        Assert.Equal([2, 2, 2, 2], server.RootFieldsPerRequest);
    }

    [Fact]
    public async Task ItemRejectedOnItsOwn_FailsAlone()
    {
        var server = new ScriptedServer { RejectName = "huge", Rejection = Rejection.ErrorsWithStatus400 };
        var client = CreateClient(server);
        var results = new List<string>();

        var result = await client.CreateBatch()
            .Query(c => c.Query.Hello("a").Select().OnResult(r => results.Add(r)))
            .Query(c => c.Query.Hello("huge").Select().OnResult(r => results.Add(r)))
            .Query(c => c.Query.Hello("c").Select().OnResult(r => results.Add(r)))
            .ExecuteWithResultAsync();

        Assert.Equal(["hello b0", "hello b2"], results);
        Assert.Equal("Rejected", Assert.Single(result.Errors).Message);
        Assert.Empty(result.Failures);
    }

    [Theory]
    [InlineData(Rejection.ErrorsWithStatus400)]
    [InlineData(Rejection.PayloadTooLarge)]
    public async Task ItemRejectedOnItsOwn_DoesNotLowerTheAcceptedSize(Rejection rejection)
    {
        var server = new ScriptedServer { RejectName = "huge", Rejection = rejection };
        var client = CreateClient(server);
        var batch = client.CreateBatch();
        for (var i = 0; i < 16; i++)
        {
            var name = i == 5 ? "huge" : "n" + i;
            batch.Query(c => c.Query.Hello(name).Select().OnResult(_ => { }));
        }

        await batch.ExecuteWithResultAsync();

        Assert.Null(client.GraphClient.AcceptedBatchSize);
    }

    [Fact]
    public async Task SuccessResponseThatIsNotGraphQL_IsAFailure()
    {
        var server = new ScriptedServer { EmptyName = "empty" };
        var client = CreateClient(server);
        var called = false;

        var result = await client.CreateBatch()
            .Query(c => c.Query.Hello("empty").Select().OnResult(_ => called = true))
            .ExecuteWithResultAsync();

        Assert.False(called);
        var failure = Assert.IsType<GraphQueryRequestException>(Assert.Single(result.Failures));
        Assert.Contains("not a GraphQL response", failure.Message);
    }

    [Fact]
    public async Task ItemTooLargeOnItsOwn_IsAFailure()
    {
        var server = new ScriptedServer { RejectName = "huge", Rejection = Rejection.PayloadTooLarge };
        var client = CreateClient(server);

        var result = await client.CreateBatch()
            .Query(c => c.Query.Hello("a").Select().OnResult(_ => { }))
            .Query(c => c.Query.Hello("huge").Select().OnResult(_ => { }))
            .ExecuteWithResultAsync();

        var failure = Assert.IsType<GraphQueryRequestException>(Assert.Single(result.Failures));
        Assert.Contains("RequestEntityTooLarge", failure.Message);
        Assert.Contains("b1:hello", failure.GraphQLQuery);
    }

    [Fact]
    public async Task SplitOnRejectionOff_FailsTheWholeRequest()
    {
        var server = new ScriptedServer { MaxRootFields = 1, Rejection = Rejection.ErrorsWithStatus400 };
        var client = CreateClient(server);
        var called = false;

        var result = await client.CreateBatch(o => o.SplitOnRejection = false)
            .Query(c => c.Query.Hello("a").Select().OnResult(_ => called = true))
            .Query(c => c.Query.Hello("b").Select().OnResult(_ => called = true))
            .ExecuteWithResultAsync();

        Assert.False(called);
        Assert.Single(result.Errors);
        Assert.Single(server.RootFieldsPerRequest);
    }

    [Fact]
    public async Task TransportFailure_FailsItsChunk_OtherChunksStillMap()
    {
        var server = new ScriptedServer { FailName = "boom" };
        var client = CreateClient(server);
        var results = new List<string>();

        var exception = await Assert.ThrowsAsync<GraphQueryRequestException>(() => client
            .CreateBatch(o => o.MaxItemsPerRequest = 1)
            .Query(c => c.Query.Hello("a").Select().OnResult(r => results.Add(r)))
            .Query(c => c.Query.Hello("boom").Select().OnResult(r => results.Add(r)))
            .ExecuteAsync());

        Assert.Equal(["hello b0"], results);
        Assert.Contains("InternalServerError", exception.Message);
    }

    [Fact]
    public async Task ErrorsInSeveralRequests_ThrowAnAggregateException()
    {
        var server = new ScriptedServer { FieldErrorName = "bad" };
        var client = CreateClient(server);

        var exception = await Assert.ThrowsAsync<AggregateException>(() => client
            .CreateBatch(o => o.MaxItemsPerRequest = 1)
            .Query(c => c.Query.Hello("bad").Select().OnResult(_ => { }))
            .Query(c => c.Query.Hello("bad").Select().OnResult(_ => { }))
            .ExecuteAsync());

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.All(exception.InnerExceptions, e => Assert.IsType<GraphQueryExecutionException>(e));
    }

    [Fact]
    public async Task DataNullWithAPathError_LeavesEveryItemOfTheRequestWithoutAResult()
    {
        var server = new ScriptedServer { NullDataName = "bad" };
        var client = CreateClient(server);
        var called = false;

        var result = await client.CreateBatch()
            .Query(c => c.Query.Hello("a").Select().OnResult(_ => called = true))
            .Query(c => c.Query.Hello("bad").Select().OnResult(_ => called = true))
            .ExecuteWithResultAsync();

        Assert.False(called);
        Assert.Equal("b1", Assert.Single(result.Errors).Path[0].ToString());
        Assert.Single(server.RootFieldsPerRequest);
    }

    [Fact]
    public async Task MutationRejected_IsSplitAndSentInOrder()
    {
        var server = new ScriptedServer { MaxRootFields = 1, Rejection = Rejection.ErrorsWithStatus400 };
        var client = CreateClient(server);
        var results = new List<string>();

        await client.CreateBatch()
            .Query(c => c.Mutation.SetName("a").Select().OnResult(r => results.Add(r)))
            .Query(c => c.Mutation.SetName("b").Select().OnResult(r => results.Add(r)))
            .Query(c => c.Mutation.SetName("c").Select().OnResult(r => results.Add(r)))
            .ExecuteAsync();

        Assert.Equal(["setName b0", "setName b1", "setName b2"], results);
        Assert.Equal(["b0", "b1", "b2"], server.AcceptedAliases);
    }

    [Fact]
    public async Task CancellationWhileSending_InvokesNoCallbacks()
    {
        var server = new ScriptedServer { HoldRequests = true };
        var client = CreateClient(server);
        var called = false;
        using var cancellation = new CancellationTokenSource();

        var execution = client.CreateBatch()
            .Query(c => c.Query.Hello("a").Select().OnResult(_ => called = true))
            .ExecuteAsync(cancellation.Token);
        await server.RequestArrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        Assert.False(called);
    }

    private static async Task<List<string>> ExecuteHellosAsync(SampleClient client, int count,
        int maxDegreeOfParallelism = 4)
    {
        var results = new List<string>();
        var batch = client.CreateBatch(o => o.MaxDegreeOfParallelism = maxDegreeOfParallelism);
        for (var i = 0; i < count; i++)
        {
            var name = "n" + i;
            batch.Query(c => c.Query.Hello(name).Select().OnResult(r => results.Add(r)));
        }

        await batch.ExecuteAsync();
        return results;
    }

    private static SampleClient CreateClient(ScriptedServer server)
    {
        return new SampleClient(new HttpClient(server) { BaseAddress = new Uri("https://localhost/graphql/") },
            Options.Create(new GraphClientOptions()), new ServiceCollection().BuildServiceProvider());
    }

    [GeneratedRegex(@"(b\d+):(hello|setName)\(name:\$(\w+)")]
    private static partial Regex RootField();

    private sealed class ScriptedServer : HttpMessageHandler
    {
        private readonly object sync = new();

        public int MaxRootFields { get; init; } = int.MaxValue;
        public Rejection Rejection { get; init; } = Rejection.PayloadTooLarge;
        public string? RejectName { get; init; }
        public string? FailName { get; init; }
        public string? FieldErrorName { get; init; }
        public string? NullDataName { get; init; }
        public string? EmptyName { get; init; }
        public bool HoldRequests { get; init; }

        public List<int> RootFieldsPerRequest { get; } = [];
        public List<string> AcceptedAliases { get; } = [];
        public TaskCompletionSource RequestArrived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var query = body.RootElement.GetProperty("query").GetString()!;
            var variables = body.RootElement.GetProperty("variables");
            var fields = RootField().Matches(query)
                .Select(m => (Alias: m.Groups[1].Value, Field: m.Groups[2].Value,
                    Name: variables.GetProperty(m.Groups[3].Value).GetString()))
                .ToList();

            lock (sync)
            {
                RootFieldsPerRequest.Add(fields.Count);
            }

            if (HoldRequests)
            {
                RequestArrived.TrySetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            if (fields.Count > MaxRootFields || fields.Any(f => f.Name == RejectName))
            {
                return Rejection switch
                {
                    Rejection.PayloadTooLarge => Respond(HttpStatusCode.RequestEntityTooLarge, "Too large"),
                    Rejection.ErrorsWithStatus400 => Respond(HttpStatusCode.BadRequest,
                        """{"errors":[{"message":"Rejected"}]}"""),
                    _ => Respond(HttpStatusCode.OK, """{"errors":[{"message":"Rejected"}]}""")
                };
            }

            if (fields.Any(f => f.Name == EmptyName))
            {
                return Respond(HttpStatusCode.OK, "");
            }

            if (fields.Any(f => f.Name == FailName))
            {
                return Respond(HttpStatusCode.InternalServerError, "Server exploded");
            }

            if (fields.FirstOrDefault(f => f.Name == NullDataName) is { Alias: not null } nullField)
            {
                return Respond(HttpStatusCode.OK,
                    $$"""{"data":null,"errors":[{"message":"Not null","path":["{{nullField.Alias}}"]}]}""");
            }

            lock (sync)
            {
                AcceptedAliases.AddRange(fields.Select(f => f.Alias));
            }

            var data = string.Join(",", fields.Select(f =>
                f.Name == FieldErrorName ? $"\"{f.Alias}\":null" : $"\"{f.Alias}\":\"{f.Field} {f.Alias}\""));
            var errors = fields.Where(f => f.Name == FieldErrorName)
                .Select(f => $$"""{"message":"Bad","path":["{{f.Alias}}"]}""").ToList();

            var errorsMember = errors.Count == 0 ? "" : ",\"errors\":[" + string.Join(",", errors) + "]";
            return Respond(HttpStatusCode.OK, "{\"data\":{" + data + "}" + errorsMember + "}");
        }

        private static HttpResponseMessage Respond(HttpStatusCode status, string body)
        {
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
