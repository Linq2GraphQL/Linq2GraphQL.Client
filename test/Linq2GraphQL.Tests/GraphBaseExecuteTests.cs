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

        private static QueryNode CreateNode()
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
