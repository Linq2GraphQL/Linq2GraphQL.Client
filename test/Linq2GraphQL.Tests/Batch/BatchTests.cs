using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;

namespace Linq2GraphQL.Tests.Batch;

public class BatchTests : IDisposable
{
    private readonly BatchTestServer server = new();
    private readonly SampleClient client;

    public BatchTests()
    {
        client = server.CreateClient();
    }

    public void Dispose()
    {
        server.Dispose();
    }

    [Fact]
    public async Task TwoQueries_AreSentAsOneRequest()
    {
        string? jocke = null;
        string? kalle = null;

        await client.CreateBatch()
            .Query(c => c.Query.Hello("Jocke").Select().OnResult(r => jocke = r))
            .Query(c => c.Query.Hello("Kalle").Select().OnResult(r => kalle = r))
            .ExecuteAsync();

        Assert.Equal("Hello, Jocke!", jocke);
        Assert.Equal("Hello, Kalle!", kalle);
        var operation = Assert.Single(server.Operations);
        Assert.Contains("b0:hello(name:$b0_name_1_1", operation.Query);
        Assert.Contains("b1:hello(name:$b1_name_1_1", operation.Query);
    }

    [Fact]
    public async Task DifferentShapes_EachGetTheirProjection()
    {
        List<Customer>? customers = null;
        List<Guid>? orderIds = null;
        List<IAnimal>? animals = null;
        int orderCount = -1;

        await client.CreateBatch()
            .Query(c => c.Query.Customers().Select().OnResult(r => customers = r))
            .Query(c => c.Query.Orders(first: 2).Select(e => e.Nodes.Select(n => n.OrderId).ToList())
                .OnResult(r => orderIds = r))
            .Query(c => c.Query.Animals()
                .Include(e => e.Nodes.Select(n => n.Spider()))
                .Select(e => e.Nodes)
                .OnResult(r => animals = r))
            .Query(c => c.Query.Orders().Select(e => e.Nodes.Count()).OnResult(r => orderCount = r))
            .ExecuteAsync();

        Assert.NotEmpty(customers!);
        Assert.Equal(2, orderIds!.Count);
        Assert.Contains(animals!, a => a is Spider { Speed: 11 });
        Assert.True(orderCount > 0);
        Assert.Single(server.Operations);
    }

    [Fact]
    public async Task QueryIf_False_AddsNothing()
    {
        var called = false;

        await client.CreateBatch()
            .QueryIf(c => c.Query.Hello().Select().OnResult(_ => called = true), false)
            .QueryIf(c => c.Query.Hello().Select().OnResult(_ => called = true), () => false)
            .ExecuteAsync();

        Assert.False(called);
        Assert.Empty(server.Operations);
    }

    [Fact]
    public async Task QueryIf_ConditionIsEvaluatedWhenTheBatchExecutes()
    {
        var include = false;
        string? result = null;
        var batch = client.CreateBatch()
            .QueryIf(c => c.Query.Hello().Select().OnResult(r => result = r), () => include);

        include = true;
        await batch.ExecuteAsync();

        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public async Task FactoryReturningNull_AddsNothing()
    {
        string? result = null;

        await client.CreateBatch()
            .Query(_ => null!)
            .Query(c => c.Query.Hello().Select().OnResult(r => result = r))
            .ExecuteAsync();

        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public async Task FieldError_OtherItemsGetTheirResults_ThenThrows()
    {
        string? hello = null;
        var failedItemResult = "not called";

        var exception = await Assert.ThrowsAsync<GraphQueryExecutionException>(() => client.CreateBatch()
            .Query(c => c.Query.Hello().Select().OnResult(r => hello = r))
            .Query(c => c.Query.RaiseError("Boom").Select().OnResult(r => failedItemResult = r))
            .ExecuteAsync());

        Assert.Equal("Hello, World!", hello);
        Assert.Null(failedItemResult);
        Assert.Equal("Boom", Assert.Single(exception.Errors).Message);
        Assert.Contains("b1:raiseError", exception.GraphQLQuery);
    }

    [Fact]
    public async Task ExecuteWithResultAsync_ReturnsErrorsInsteadOfThrowing()
    {
        string? hello = null;

        var result = await client.CreateBatch()
            .Query(c => c.Query.Hello().Select().OnResult(r => hello = r))
            .Query(c => c.Query.RaiseError("Boom").Select().OnResult(_ => { }))
            .ExecuteWithResultAsync();

        Assert.Equal("Hello, World!", hello);
        Assert.True(result.HasErrors);
        Assert.Empty(result.Failures);
        var error = Assert.Single(result.Errors);
        Assert.Equal("b1", error.Path[0].ToString());
        Assert.Throws<GraphQueryExecutionException>(result.EnsureNoErrors);
    }

    [Fact]
    public async Task NullResult_WithoutErrors_IsDeliveredAsNull()
    {
        var customer = new Customer();

        var result = await client.CreateBatch()
            .Query(c => c.Query.CustomerReturnNull().Select().OnResult(r => customer = r))
            .ExecuteWithResultAsync();

        Assert.Null(customer);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public async Task MaxItemsPerRequest_SplitsTheBatch_CallbacksKeepTheirOrder()
    {
        var calls = new List<string>();
        var batch = client.CreateBatch(o => o.MaxItemsPerRequest = 2);
        foreach (var name in new[] { "a", "b", "c", "d", "e" })
        {
            batch.Query(c => c.Query.Hello(name).Select().OnResult(r => calls.Add(r)));
        }

        await batch.ExecuteAsync();

        Assert.Equal(["Hello, a!", "Hello, b!", "Hello, c!", "Hello, d!", "Hello, e!"], calls);
        Assert.Equal([2, 2, 1], server.Operations.Select(o => o.Query.Split(":hello").Length - 1).Order().Reverse());
    }

    [Fact]
    public async Task Mutations_AreSentInOneMutationOperation()
    {
        var results = new List<string>();

        await client.CreateBatch()
            .Query(c => c.Mutation.SetName("A").Select().OnResult(r => results.Add(r)))
            .Query(c => c.Mutation.SetName("B").Select().OnResult(r => results.Add(r)))
            .ExecuteAsync();

        Assert.Equal(["New Name is: A", "New Name is: B"], results);
        Assert.StartsWith("mutation", Assert.Single(server.Operations).Query);
    }

    [Fact]
    public async Task QueriesAndMutations_AreSentAsSeparateOperations()
    {
        string? hello = null;
        string? name = null;

        await client.CreateBatch()
            .Query(c => c.Query.Hello().Select().OnResult(r => hello = r))
            .Query(c => c.Mutation.SetName("A").Select().OnResult(r => name = r))
            .ExecuteAsync();

        Assert.Equal("Hello, World!", hello);
        Assert.Equal("New Name is: A", name);
        Assert.Equal(["mutation", "query"], server.Operations.Select(o => o.Query.Split('(', '{')[0]).Order());
    }

    [Fact]
    public async Task ParallelMode_SendsEachQueryOnItsOwn()
    {
        string? jocke = null;
        string? error = "not called";

        var result = await client.CreateBatch(o => o.Mode = GraphBatchMode.Parallel)
            .Query(c => c.Query.Hello("Jocke").Select().OnResult(r => jocke = r))
            .Query(c => c.Query.RaiseError("Boom").Select().OnResult(r => error = r))
            .ExecuteWithResultAsync();

        Assert.Equal("Hello, Jocke!", jocke);
        Assert.Null(error);
        Assert.Equal("Boom", Assert.Single(result.Errors).Message);
        Assert.Equal(2, server.Operations.Count);
        Assert.All(server.Operations, o => Assert.DoesNotContain("b0", o.Query));
    }

    [Fact]
    public async Task BatchedQuery_CanStillBeExecutedOnItsOwn()
    {
        var query = client.Query.Hello("Jocke").Select();

        await client.CreateBatch().Query(_ => query.OnResult(_ => { })).ExecuteAsync();

        Assert.Equal("Hello, Jocke!", query.BaseResult);
        Assert.Equal("Hello, Jocke!", await query.ExecuteAsync());
        Assert.Contains("hello(name:$name_1_1", server.Operations[^1].Query);
    }

    [Fact]
    public async Task SameQueryTwiceInABatch_GetsTwoResults()
    {
        var query = client.Query.Hello("Jocke").Select();
        var results = new List<string>();

        await client.CreateBatch()
            .Query(_ => query.OnResult(r => results.Add(r)))
            .Query(_ => query.OnResult(r => results.Add(r)))
            .ExecuteAsync();

        Assert.Equal(["Hello, Jocke!", "Hello, Jocke!"], results);
    }

    [Fact]
    public async Task SameItemTwice_Throws()
    {
        var item = client.Query.Hello().Select().OnResult(_ => { });

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateBatch()
            .Query(_ => item)
            .Query(_ => item)
            .ExecuteAsync());
    }

    [Fact]
    public async Task QueryAddedAfterAMutation_IsSentAfterIt()
    {
        await client.CreateBatch()
            .Query(c => c.Query.Hello("a").Select().OnResult(_ => { }))
            .Query(c => c.Mutation.SetName("m").Select().OnResult(_ => { }))
            .Query(c => c.Query.Hello("b").Select().OnResult(_ => { }))
            .ExecuteAsync();

        Assert.Equal(["query", "mutation", "query"], server.Operations.Select(o => o.Query.Split('(', '{')[0]));
        Assert.Contains("b2:hello", server.Operations[2].Query);
    }

    [Fact]
    public async Task Aliases_CountSkippedQueries()
    {
        var result = await client.CreateBatch()
            .QueryIf(c => c.Query.Hello().Select().OnResult(_ => { }), false)
            .Query(c => c.Query.RaiseError("Boom").Select().OnResult(_ => { }))
            .ExecuteWithResultAsync();

        Assert.Equal("b1", Assert.Single(result.Errors).Path[0].ToString());
    }

    [Fact]
    public async Task QueryInitializedBeforeBatching_IsRenamedForTheBatch()
    {
        string? result = null;
        var query = client.Query.Hello("Jocke").Select();
        await query.GetRequestAsync();

        await client.CreateBatch()
            .Query(_ => query.OnResult(r => result = r))
            .ExecuteAsync();

        Assert.Equal("Hello, Jocke!", result);
        Assert.Contains("$b0_name_1_1", Assert.Single(server.Operations).Query);
    }

    [Fact]
    public async Task ExecutingABatchTwice_Throws()
    {
        var batch = client.CreateBatch().Query(c => c.Query.Hello().Select().OnResult(_ => { }));
        await batch.ExecuteAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => batch.ExecuteAsync());
    }

    [Fact]
    public async Task DerivedClient_RequestHooksRunOnTheBatchRequest()
    {
        var userClient = client.WithRequestConfiguration((request, _) =>
        {
            request.Headers.Add("X-Batch", "user");
            return ValueTask.CompletedTask;
        });

        await userClient.CreateBatch()
            .Query(c => c.Query.Hello().Select().OnResult(_ => { }))
            .ExecuteAsync();

        Assert.Equal(["user"], Assert.Single(server.Operations).BatchHeader);
    }

    [Fact]
    public async Task ItemsFromTwoClients_AreSentWithTheirOwnHooks()
    {
        var userClient = client.WithRequestConfiguration((request, _) =>
        {
            request.Headers.Add("X-Batch", "user");
            return ValueTask.CompletedTask;
        });

        await client.CreateBatch()
            .Query(c => c.Query.Hello().Select().OnResult(_ => { }))
            .Query(_ => userClient.Query.Hello().Select().OnResult(_ => { }))
            .ExecuteAsync();

        Assert.Equal(2, server.Operations.Count);
        Assert.Single(server.Operations, o => o.BatchHeader.SequenceEqual(["user"]));
    }

    [Fact]
    public async Task CancelledToken_InvokesNoCallbacks()
    {
        var called = false;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.CreateBatch()
            .Query(c => c.Query.Hello().Select().OnResult(_ => called = true))
            .ExecuteAsync(cancellation.Token));

        Assert.False(called);
    }

    [Fact]
    public async Task OnResultAsync_IsAwaitedBeforeTheNextCallback()
    {
        var calls = new List<string>();

        await client.CreateBatch()
            .Query(c => c.Query.Hello("a").Select().OnResultAsync(async r =>
            {
                await Task.Delay(20);
                calls.Add(r);
            }))
            .Query(c => c.Query.Hello("b").Select().OnResult(r => calls.Add(r)))
            .ExecuteAsync();

        Assert.Equal(["Hello, a!", "Hello, b!"], calls);
    }

    [Fact]
    public async Task ACallbackThatThrows_StopsTheBatch()
    {
        var secondCalled = false;

        await Assert.ThrowsAsync<FormatException>(() => client.CreateBatch()
            .Query(c => c.Query.Hello().Select().OnResult(_ => throw new FormatException()))
            .Query(c => c.Query.Hello().Select().OnResult(_ => secondCalled = true))
            .ExecuteAsync());

        Assert.False(secondCalled);
    }

    [Fact]
    public async Task ClientInterface_CanCreateABatch()
    {
        ISampleClient sampleClient = client;
        string? result = null;

        await GraphBatch.Create(sampleClient)
            .Query(c => c.Query.Hello().Select().OnResult(r => result = r))
            .ExecuteAsync();

        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public async Task SafeMode_FetchesTheSchemaOnceForTheWholeBatch()
    {
        var batch = client.CreateBatch();
        for (var i = 0; i < 5; i++)
        {
            batch.Query(c => c.Query.Orders(first: 1).Select(e => e.Nodes).OnResult(_ => { }));
        }

        await batch.ExecuteAsync();

        Assert.Equal(1, server.SchemaQueryCount);
    }

    [Fact]
    public async Task ManyBatchesInParallel_OnOneClient_AllGetTheirResults()
    {
        var results = new string?[20];

        await Task.WhenAll(Enumerable.Range(0, 20).Select(i => client.CreateBatch()
            .Query(c => c.Query.Hello("n" + i).Select().OnResult(r => results[i] = r))
            .Query(c => c.Query.Customers().Select(e => e.Count).OnResult(_ => { }))
            .ExecuteAsync()));

        Assert.Equal(Enumerable.Range(0, 20).Select(i => $"Hello, n{i}!"), results);
    }

    [Fact]
    public void InvalidOptions_AreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => client.CreateBatch(o => o.MaxItemsPerRequest = 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.CreateBatch(o => o.MaxDegreeOfParallelism = 0));
    }
}
