using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Linq2GraphQL.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Linq2GraphQL.Tests;

/// <summary>
///     Response-envelope handling: the client streams the response into a UTF-8 buffer
///     and parses with Utf8JsonReader (GraphEnvelopeReader) — no UTF-16 string copy,
///     no JsonDocument lifetime issues. Timeout semantics are pinned here too.
/// </summary>
public class QueryExecutorStreamingTests
{
    [Fact]
    public async Task MultiRootPayload_OnlyTargetFieldDeserialized_IncompatibleSiblingsIgnored()
    {
        // "other" is a JSON number — deserializing it into an object type would throw,
        // proving it is skipped rather than touched.
        var executor = CreateExecutor(
            """{"data":{"other":42,"customer":{"name":"n"}}}""",
            out var client);

        var request = new GraphQLRequest { Query = "query { customer }" };
        var result = await executor.ExecuteRawAsync("customer", request);

        Assert.Null(result.Errors);
        Assert.Equal("n", result.Data.name);
    }

    [Fact]
    public async Task DataNull_ReturnsDefault()
    {
        var executor = CreateExecutor("""{"data":null}""", out _);

        var result = await executor.ExecuteRawAsync("customer", new GraphQLRequest { Query = "query { customer }" });

        Assert.Null(result.Data);
        Assert.Null(result.Errors);
    }

    [Fact]
    public async Task TargetFieldNull_ReturnsDefault()
    {
        var executor = CreateExecutor("""{"data":{"customer":null}}""", out _);

        var result = await executor.ExecuteRawAsync("customer", new GraphQLRequest { Query = "query { customer }" });

        Assert.Null(result.Data);
    }

    [Fact]
    public async Task ErrorsWithPartialData_ResultCarriesBoth()
    {
        var executor = CreateExecutor(
            """{"data":{"customer":null},"errors":[{"message":"boom"}],"extensions":{"trace":"x"}}""",
            out _);

        var result = await executor.ExecuteRawAsync("customer", new GraphQLRequest { Query = "query { customer }" });

        Assert.NotNull(result.Errors);
        Assert.Equal("boom", result.Errors.Single().Message);
        Assert.NotNull(result.Extensions);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Errors_ExecuteRequestAsync_Throws()
    {
        var executor = CreateExecutor("""{"errors":[{"message":"boom"}]}""", out _);

        await Assert.ThrowsAsync<GraphQueryExecutionException>(() =>
            executor.ExecuteRequestAsync("customer", new GraphQLRequest { Query = "query { customer }" }));
    }

    [Fact]
    public async Task StallingBody_TimesOut_TaskCanceledWithTimeoutInner()
    {
        var executor = CreateExecutor(new StallingStream(), out var client);
        client.HttpClient.Timeout = TimeSpan.FromMilliseconds(200);

        var stopwatch = Stopwatch.StartNew();
        var exception = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            executor.ExecuteRequestAsync("customer", new GraphQLRequest { Query = "query { customer }" }));

        // Timeout mapped like HttpClient: inner TimeoutException, fast failure.
        Assert.IsType<TimeoutException>(exception.InnerException);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Took {stopwatch.Elapsed}");
    }

    [Fact]
    public async Task CallerCancellation_NotMappedAsTimeout()
    {
        var executor = CreateExecutor(new StallingStream(), out _);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        var exception = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            executor.ExecuteRequestAsync("customer", new GraphQLRequest { Query = "query { customer }" }, cts.Token));

        Assert.NotEqual(typeof(TimeoutException), exception.InnerException?.GetType());
    }

    [Fact]
    public async Task RequestBody_GoldenBytes_Unchanged()
    {
        const string response = """{"data":{"customer":null}}""";
        byte[]? captured = null;
        var handler = new CapturingHandler(HttpStatusCode.OK, response, bytes => captured = bytes);
        var executor = CreateExecutor(handler);

        await executor.ExecuteRequestAsync("customer", new GraphQLRequest { Query = "query { hello }" });

        Assert.NotNull(captured);
        Assert.Equal("""{"query":"query { hello }"}""", Encoding.UTF8.GetString(captured));
    }

    [Fact]
    public async Task HttpError_ThrowsRequestException_WithTruncatedBody()
    {
        var longBody = new string('x', 10_000);
        var executor = CreateExecutor(new StaticHandler(HttpStatusCode.InternalServerError, longBody));

        var exception = await Assert.ThrowsAsync<GraphQueryRequestException>(() =>
            executor.ExecuteRequestAsync("customer", new GraphQLRequest { Query = "query { hello }" }));

        Assert.Contains("InternalServerError", exception.Message);
        Assert.True(exception.Message.Length < longBody.Length,
            "Error message should not embed the full body.");
    }

    [Fact]
    public async Task HttpError_ThrowsRequestException_WithShortBody()
    {
        const string body = "upstream unavailable";
        var executor = CreateExecutor(new StaticHandler(HttpStatusCode.BadGateway, body));

        var exception = await Assert.ThrowsAsync<GraphQueryRequestException>(() =>
            executor.ExecuteRequestAsync("customer", new GraphQLRequest { Query = "query { hello }" }));

        Assert.Contains("BadGateway", exception.Message);
        Assert.Contains(body, exception.Message);
        Assert.DoesNotContain("truncated", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static QueryExecutor<QueryExecutorStreamingTests.Customer> CreateExecutor(string response,
        out GraphClient client)
    {
        return CreateExecutor(new StaticHandler(HttpStatusCode.OK, response), out client);
    }

    private static QueryExecutor<QueryExecutorStreamingTests.Customer> CreateExecutor(HttpResponseMessage response,
        out GraphClient client)
    {
        return CreateExecutor(new LazyHandler(() => response), out client);
    }

    private static QueryExecutor<QueryExecutorStreamingTests.Customer> CreateExecutor(Stream stream,
        out GraphClient client)
    {
        return CreateExecutor(new LazyHandler(() =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                "application/json");
            return response;
        }), out client);
    }

    private static QueryExecutor<QueryExecutorStreamingTests.Customer> CreateExecutor(HttpMessageHandler handler,
        out GraphClient client)
    {
        client = CreateClient(handler);
        return new QueryExecutor<QueryExecutorStreamingTests.Customer>(client);
    }

    private static QueryExecutor<QueryExecutorStreamingTests.Customer> CreateExecutor(HttpMessageHandler handler)
    {
        return new QueryExecutor<QueryExecutorStreamingTests.Customer>(CreateClient(handler));
    }

    private static GraphClient CreateClient(HttpMessageHandler handler)
    {
        return new GraphClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/graphql/")
        }, Microsoft.Extensions.Options.Options.Create(new GraphClientOptions()),
            new ServiceCollection().BuildServiceProvider());
    }

    private sealed class Customer
    {
        public string name { get; set; } = null!;
    }

    private sealed class StaticHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode) { Content = new StringContent(body, Encoding.UTF8) };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                "application/json");
            return Task.FromResult(response);
        }
    }

    private sealed class LazyHandler(Func<HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responder());
        }
    }

    private sealed class CapturingHandler(HttpStatusCode statusCode, string body, Action<byte[]> onBody)
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            onBody(await request.Content!.ReadAsByteArrayAsync(cancellationToken));
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            return response;
        }
    }

    private sealed class StallingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }

        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
