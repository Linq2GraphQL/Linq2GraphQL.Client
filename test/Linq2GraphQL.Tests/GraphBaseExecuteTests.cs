using System.Linq.Expressions;
using Linq2GraphQL.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Linq2GraphQL.Tests;

public class GraphBaseExecuteTests
{
    [Fact]
    public void IdentitySelector_IsNotCompiled()
    {
        var exec = new TestExecute(CreateClient(), e => e);

        Assert.False(exec.HasCompiledMapper);
    }

    [Fact]
    public async Task InitQueryAsync_ConcurrentCalls_InitializeExactlyOnce()
    {
        // Safe mode ON: initialization runs the introspection request. N concurrent
        // GetRequestAsync calls must produce exactly one introspection round trip.
        var handler = new CountingHandler("""{"data":{"__schema":{"types":[]}}}""");
        var client = new GraphClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/graphql/")
        }, Microsoft.Extensions.Options.Options.Create(new GraphClientOptions { UseSafeMode = true }),
            new ServiceCollection().AddMemoryCache().BuildServiceProvider());
        var exec = new TestExecute(client, e => e);

        var requests = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => exec.GetRequestAsync()));

        Assert.Equal(1, handler.RequestCount);
        Assert.All(requests, request => Assert.Equal(requests[0].Query, request.Query));
    }

    private sealed class CountingHandler(string responseJson) : HttpMessageHandler
    {
        public int RequestCount;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref RequestCount);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private const string EmptySchemaJson = """{"data":{"__schema":{"types":[]}}}""";

    [Fact]
    public async Task GetRequestAsync_CancelledDuringIntrospection_ThrowsOperationCanceled()
    {
        var handler = new ScriptedHandler(HangUntilCancelled);
        var exec = new TestExecute(CreateSafeModeClient(handler), e => e);
        using var cts = new CancellationTokenSource();

        var pending = exec.GetRequestAsync(cts.Token);
        await handler.FirstRequestStarted;
        cts.Cancel();

        await AssertCancelledAsync(pending);
    }

    [Fact]
    public async Task GetRequestAsync_PreCancelledToken_SendsNoRequest()
    {
        var handler = new ScriptedHandler(Respond(EmptySchemaJson));
        var exec = new TestExecute(CreateSafeModeClient(handler), e => e);

        await AssertCancelledAsync(exec.GetRequestAsync(new CancellationToken(true)));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task GetRequestAsync_AfterCancelledIntrospection_RetriesAndSucceeds()
    {
        // A cancelled schema fetch must not be cached, and must not leave the query half-initialized.
        var handler = new ScriptedHandler(HangUntilCancelled, Respond(EmptySchemaJson));
        var exec = new TestExecute(CreateSafeModeClient(handler), e => e);
        using var cts = new CancellationTokenSource();

        var cancelled = exec.GetRequestAsync(cts.Token);
        await handler.FirstRequestStarted;
        cts.Cancel();
        await AssertCancelledAsync(cancelled);

        var request = await exec.GetRequestAsync(CancellationToken.None);

        Assert.NotNull(request.Query);
        Assert.Equal(2, handler.RequestCount);
        Assert.NotNull(exec.Schema);
    }

    [Fact]
    public async Task GetRequestAsync_CancelWhileWaitingForLock_DoesNotDisturbLockHolder()
    {
        // The first caller holds the init lock while its introspection request is blocked on a gate it
        // does not cancel. A second caller waiting for the lock must be able to give up on its own.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new ScriptedHandler(async _ =>
        {
            await gate.Task;
            return Ok(EmptySchemaJson);
        });
        var exec = new TestExecute(CreateSafeModeClient(handler), e => e);

        var holder = exec.GetRequestAsync(CancellationToken.None);
        await handler.FirstRequestStarted;

        using var cts = new CancellationTokenSource();
        var waiter = exec.GetRequestAsync(cts.Token);
        cts.Cancel();

        await AssertCancelledAsync(waiter);
        Assert.False(holder.IsCompleted);

        gate.SetResult();
        var request = await holder;

        Assert.NotNull(request.Query);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GetRequestAsync_WithoutToken_StillInitializes()
    {
        // The parameterless overload is kept for binary compatibility and must behave as before.
        var handler = new ScriptedHandler(Respond(EmptySchemaJson));
        var exec = new TestExecute(CreateSafeModeClient(handler), e => e);

        var request = await exec.GetRequestAsync();

        Assert.NotNull(request.Query);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledDuringSafeModeSetup_ThrowsOperationCanceled()
    {
        var handler = new ScriptedHandler(HangUntilCancelled);
        var exec = new GraphQueryExecute<TestType, TestType>(CreateSafeModeClient(handler), OperationType.Query,
            TestExecute.CreateNode(), e => e);
        using var cts = new CancellationTokenSource();

        var pending = exec.ExecuteAsync(cts.Token);
        await handler.FirstRequestStarted;
        cts.Cancel();

        await AssertCancelledAsync(pending);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task ExecuteWithResultAsync_CancelledDuringSafeModeSetup_ThrowsOperationCanceled()
    {
        var handler = new ScriptedHandler(HangUntilCancelled);
        var exec = new GraphQueryExecute<TestType, TestType>(CreateSafeModeClient(handler), OperationType.Query,
            TestExecute.CreateNode(), e => e);
        using var cts = new CancellationTokenSource();

        var pending = exec.ExecuteWithResultAsync(cts.Token);
        await handler.FirstRequestStarted;
        cts.Cancel();

        await AssertCancelledAsync(pending);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GetSchemaForSafeModeAsync_Cancelled_IsNotCached()
    {
        var handler = new ScriptedHandler(HangUntilCancelled, Respond(EmptySchemaJson));
        var client = CreateSafeModeClient(handler);
        using var cts = new CancellationTokenSource();

        var cancelled = client.GetSchemaForSafeModeAsync(cts.Token);
        await handler.FirstRequestStarted;
        cts.Cancel();
        await AssertCancelledAsync(cancelled);

        Assert.NotNull(await client.GetSchemaForSafeModeAsync());
        Assert.NotNull(await client.GetSchemaForSafeModeAsync());
        Assert.Equal(2, handler.RequestCount);
    }

    /// <summary>
    /// The task must end as cancelled by the caller's token, promptly. Bounded so a dropped token fails the
    /// test fast — <see cref="Task.WaitAsync(TimeSpan)"/> throws <see cref="TimeoutException"/>, which is no
    /// <see cref="OperationCanceledException"/> — and the client's HTTP timeout is infinite (see
    /// <see cref="CreateSafeModeClient"/>) so a timeout can never pass as a cancellation.
    /// </summary>
    private static async Task AssertCancelledAsync(Task task)
    {
        var ex = await Record.ExceptionAsync(() => task.WaitAsync(TimeSpan.FromSeconds(10)));

        Assert.IsAssignableFrom<OperationCanceledException>(ex);
    }

    private static GraphClient CreateSafeModeClient(HttpMessageHandler handler)
    {
        return new GraphClient(new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost/graphql/"),
                Timeout = Timeout.InfiniteTimeSpan
            },
            Microsoft.Extensions.Options.Options.Create(new GraphClientOptions { UseSafeMode = true }),
            new ServiceCollection().AddMemoryCache().BuildServiceProvider());
    }

    private static HttpResponseMessage Ok(string json) => new(System.Net.HttpStatusCode.OK)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    };

    private static Func<CancellationToken, Task<HttpResponseMessage>> Respond(string json) =>
        _ => Task.FromResult(Ok(json));

    private static async Task<HttpResponseMessage> HangUntilCancelled(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        throw new InvalidOperationException("unreachable");
    }

    /// <summary>Plays one response script per request, in order; the last one repeats.</summary>
    private sealed class ScriptedHandler(params Func<CancellationToken, Task<HttpResponseMessage>>[] script)
        : HttpMessageHandler
    {
        private readonly TaskCompletionSource firstRequestStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int requestCount;

        public int RequestCount => Volatile.Read(ref requestCount);

        public Task FirstRequestStarted => firstRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var index = Interlocked.Increment(ref requestCount) - 1;
            firstRequestStarted.TrySetResult();
            return script[Math.Min(index, script.Length - 1)](cancellationToken);
        }
    }

    [Fact]
    public void NonIdentitySelector_IsCompiled()
    {
        var exec = new TestExecute(CreateClient(), e => new GraphBaseExecuteTests.TestType { Name = e.Name + "!" });

        Assert.True(exec.HasCompiledMapper);
    }

    [Fact]
    public void ConvertResult_IdentitySelector_ReturnsSameInstance()
    {
        var exec = new TestExecute(CreateClient(), e => e);
        var data = new TestType { Name = "x" };

        Assert.Same(data, exec.ConvertResult(data));
    }

    [Fact]
    public void ConvertResult_NullData_ReturnsDefault()
    {
        var exec = new TestExecute(CreateClient(), e => e);

        Assert.Null(exec.ConvertResult(null!));
    }

    [Fact]
    public async Task GetRequestAsJsonAsync_GoldenQuery()
    {
        var exec = new TestExecute(CreateClient(), e => e);

        var json = await exec.GetRequestAsJsonAsync();

        // Root field without arguments: no variable declarations, empty variables object.
        var nl = Environment.NewLine;
        var expected = $"query{{ {nl}hello{nl}{{{nl}}}{nl}{nl}}}{nl}{nl}{{}}";
        Assert.Equal(expected, json);
    }

    private static GraphClient CreateClient()
    {
        return new GraphClient(new HttpClient { BaseAddress = new Uri("https://localhost/graphql/") },
            Microsoft.Extensions.Options.Options.Create(new GraphClientOptions()),
            new ServiceCollection().AddMemoryCache().BuildServiceProvider());
    }

    private sealed class TestExecute(GraphClient client, Expression<Func<TestType, TestType>> selector)
        : GraphBaseExecute<TestType, TestType>(client, OperationType.Query, CreateNode(), selector)
    {
        public bool HasCompiledMapper => mapper != null;

        internal static QueryNode CreateNode()
        {
            var member = typeof(TestFields).GetProperty(nameof(TestFields.Hello));
            return new QueryNode(member, topLevel: true);
        }
    }

    private static class TestFields
    {
        [GraphQLMember("hello")]
        public static TestType Hello { get; } = new();
    }

    private sealed class TestType
    {
        public string Name { get; set; } = null!;
    }
}
