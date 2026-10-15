using System.Net;
using System.Text;
using Linq2GraphQL.Client;
using System.Reactive.Linq;
using Linq2GraphQL.Client.Subscriptions;

namespace Linq2GraphQL.Tests;

/// <summary>
///     Direct tests of SSEClient — the SSE transport loop, dispatch semantics, connect
///     handshake and dispose behavior. Uses a fake HttpMessageHandler with a scriptable
///     event stream, so every branch is exercised deterministically.
/// </summary>
public class SseClientTests
{
    private static SSEClient CreateClient(FakeSseHandler handler, GraphClientOptions? options = null)
    {
        var graphClient = TestGraphClientFactory.Create(handler, options);
        var request = new GraphQLRequest { Query = "subscription { customerAdded { customerId } }" };
        return new SSEClient(graphClient, request);
    }

    private static (List<byte[]> payloads, List<GraphQueryExecutionException> errors, List<Exception> failures)
        Subscribe(SSEClient client)
    {
        var payloads = new List<byte[]>();
        var errors = new List<GraphQueryExecutionException>();
        var failures = new List<Exception>();
        client.Payloads.Subscribe(payloads.Add);
        client.Errors.Subscribe(errors.Add);
        client.Failures.Subscribe(failures.Add);
        return (payloads, errors, failures);
    }

    [Fact]
    public async Task DefaultAndMessageEventTypes_MapToNext()
    {
        var handler = FakeSseHandler.Streaming(out var stream);
        using var client = CreateClient(handler);
        var (payloads, _, failures) = Subscribe(client);

        _ = client.Start();
        await client.Connected.WaitAsync(TimeSpan.FromSeconds(10));
        stream.Push("event: next\ndata: \"A\"\n\n");
        stream.Push("data: \"B\"\n\n"); // no event field → SSE default "message"

        await WaitUntilAsync(() => payloads.Count == 2);
        Assert.Equal("\"A\"", Encoding.UTF8.GetString(payloads[0]));
        Assert.Equal("\"B\"", Encoding.UTF8.GetString(payloads[1]));
        Assert.Empty(failures);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int seconds = 10)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.Fail("Condition was not met before the timeout.");
    }

    [Fact]
    public async Task CrlfLineEndings_AreParsed()
    {
        var handler = FakeSseHandler.Streaming(out var stream);
        using var client = CreateClient(handler);
        var (payloads, _, failures) = Subscribe(client);

        _ = client.Start();
        await client.Connected.WaitAsync(TimeSpan.FromSeconds(10));
        stream.Push("event: next\r\ndata: {\"a\":1}\r\n\r\n");

        await WaitUntilAsync(() => payloads.Count == 1);
        Assert.Equal("{\"a\":1}", Encoding.UTF8.GetString(payloads[0]));
        Assert.Empty(failures);
    }

    [Fact]
    public async Task FrameSplitAcrossChunkBoundary_IsBufferedUntilComplete()
    {
        var handler = FakeSseHandler.Streaming(out var stream);
        using var client = CreateClient(handler);
        var (payloads, _, _) = Subscribe(client);

        _ = client.Start();
        await client.Connected.WaitAsync(TimeSpan.FromSeconds(10));
        stream.Push("data: {\"partial\":true}\n");
        await Task.Delay(100); // parser must not emit an incomplete frame
        Assert.Empty(payloads);

        stream.Push("\n"); // the blank line completes the event
        await WaitUntilAsync(() => payloads.Count == 1);
        Assert.Single(payloads);
    }

    [Fact]
    public async Task ErrorEvent_ArrayRoot_MapsToGraphQueryExecutionException()
    {
        var handler = FakeSseHandler.Streaming(out var stream);
        using var client = CreateClient(handler);
        var (_, errors, failures) = Subscribe(client);

        var started = client.Start();
        stream.Push("event: error\ndata: [{\"message\":\"boom\"}]\n\n");

        await started.WaitAsync(TimeSpan.FromSeconds(10));
        var error = Assert.Single(errors);
        Assert.Equal("boom", Assert.Single(error.Errors).Message);
        Assert.Single(failures); // failure reported exactly once, stream terminated
    }

    [Fact]
    public async Task ErrorEvent_ObjectRootErrors_MapsToGraphQueryExecutionException()
    {
        var handler = FakeSseHandler.Streaming(out var stream);
        using var client = CreateClient(handler);
        var (_, errors, _) = Subscribe(client);

        var started = client.Start();
        stream.Push("event: error\ndata: {\"errors\":[{\"message\":\"object-root\"}]}\n\n");

        await started.WaitAsync(TimeSpan.FromSeconds(10));
        var error = Assert.Single(errors);
        Assert.Equal("object-root", Assert.Single(error.Errors).Message);
    }

    [Fact]
    public async Task ErrorEvent_NonJsonPayload_FallsBackToRawText()
    {
        var handler = FakeSseHandler.Streaming(out var stream);
        using var client = CreateClient(handler);
        var (_, errors, _) = Subscribe(client);

        var started = client.Start();
        stream.Push("event: error\ndata: not-json-at-all\n\n");

        await started.WaitAsync(TimeSpan.FromSeconds(10));
        var error = Assert.Single(errors);
        Assert.Equal("not-json-at-all", Assert.Single(error.Errors).Message);
    }

    [Fact]
    public async Task CompleteEvent_CompletesStream_ExactlyOnce()
    {
        var handler = FakeSseHandler.Streaming(out var stream);
        using var client = CreateClient(handler);
        var completedCount = 0;
        client.Payloads.Subscribe(_ => { }, () => Interlocked.Increment(ref completedCount));

        var started = client.Start();
        stream.Push("event: complete\ndata: \n\n");
        await started.WaitAsync(TimeSpan.FromSeconds(10));

        // A late duplicate complete (or next) must not re-complete the subject.
        stream.Push("event: complete\ndata: \n\n");
        stream.Push("event: next\ndata: \"late\"\n\n");
        await Task.Delay(100);

        Assert.Equal(1, completedCount);
    }

    [Fact]
    public async Task UnknownEventTypes_AreIgnored_StreamContinues()
    {
        var handler = FakeSseHandler.Streaming(out var stream);
        using var client = CreateClient(handler);
        var (payloads, _, failures) = Subscribe(client);

        _ = client.Start();
        await client.Connected.WaitAsync(TimeSpan.FromSeconds(10));
        stream.Push(": keepalive comment\n\n"); // SSE comment
        stream.Push("event: ka\n\n"); // HotChocolate-style keep-alive event
        stream.Push("event: next\ndata: \"after-ka\"\n\n");

        await WaitUntilAsync(() => payloads.Count == 1);
        Assert.Single(payloads);
        Assert.Equal("\"after-ka\"", Encoding.UTF8.GetString(payloads[0]));
        Assert.Empty(failures);
    }

    [Fact]
    public async Task EofWithoutComplete_FailsTheStream()
    {
        var handler = FakeSseHandler.Streaming(out var stream);
        using var client = CreateClient(handler);
        var failures = new List<Exception>();
        client.Failures.Subscribe(failures.Add);

        var started = client.Start();
        stream.CompleteStream(); // server connection dropped without 'complete'

        await started.WaitAsync(TimeSpan.FromSeconds(10));
        var failure = Assert.Single(failures);
        // The transport wraps terminal stream failures in GraphQueryRequestException.
        Assert.IsType<GraphQueryRequestException>(failure);
        Assert.Contains("ended before", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NonSuccessStatus_FailsWithGraphQueryRequestException()
    {
        var handler = FakeSseHandler.Status(HttpStatusCode.InternalServerError, body: "nope");
        using var client = CreateClient(handler);
        var failures = new List<Exception>();
        client.Failures.Subscribe(failures.Add);
        var connectedFaults = Assert.ThrowsAnyAsync<Exception>(() => client.Connected.WaitAsync(TimeSpan.FromSeconds(5)));

        var started = client.Start();
        await started.WaitAsync(TimeSpan.FromSeconds(10));

        var failure = Assert.Single(failures);
        Assert.IsType<GraphQueryRequestException>(failure);
        Assert.Contains("InternalServerError", failure.Message);
        var connectedFault = await connectedFaults; // WhenConnected faulted with the same failure
        Assert.NotNull(connectedFault);
    }

    [Fact]
    public async Task WrongContentType_FailsTheConnection()
    {
        var handler = FakeSseHandler.Status(HttpStatusCode.OK, contentType: "application/json", body: "{}");
        using var client = CreateClient(handler);
        var failures = new List<Exception>();
        client.Failures.Subscribe(failures.Add);

        var started = client.Start();
        await started.WaitAsync(TimeSpan.FromSeconds(10));

        var failure = Assert.Single(failures);
        Assert.IsType<GraphQueryRequestException>(failure);
        Assert.Contains("content type", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IdleTimeoutSet_StalledServer_FailsTheStream()
    {
        var options = new GraphClientOptions
        {
            SubscriptionProtocol = SubscriptionProtocol.ServerSentEvents,
            SubscriptionIdleTimeout = TimeSpan.FromMilliseconds(100),
        };
        var handler = FakeSseHandler.Streaming(out var stream);
        using var client = CreateClient(handler, options);
        var failures = new List<Exception>();
        client.Failures.Subscribe(failures.Add);

        var started = client.Start();
        await started.WaitAsync(TimeSpan.FromSeconds(10));

        var failure = Assert.Single(failures);
        // The idle timeout (IOException) is wrapped in GraphQueryRequestException.
        Assert.IsType<GraphQueryRequestException>(failure);
        Assert.Contains("idle", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task KeepAliveComments_PreventIdleTimeout()
    {
        var options = new GraphClientOptions
        {
            SubscriptionProtocol = SubscriptionProtocol.ServerSentEvents,
            SubscriptionIdleTimeout = TimeSpan.FromMilliseconds(250),
        };
        var handler = FakeSseHandler.Streaming(out var stream);
        using var client = CreateClient(handler, options);
        var (payloads, _, failures) = Subscribe(client);

        _ = client.Start();
        await client.Connected.WaitAsync(TimeSpan.FromSeconds(10));
        for (var i = 0; i < 8; i++)
        {
            stream.Push(": ka\n\n");
            await Task.Delay(80); // total 640 ms > idle timeout — must stay alive
        }

        stream.Push("event: next\ndata: \"alive\"\n\n");
        await WaitUntilAsync(() => payloads.Count == 1);

        Assert.Single(payloads);
        Assert.Empty(failures);
    }

    [Fact]
    public async Task DisposeDuringPendingConnect_ConnectedCancels_NoFailureReported()
    {
        var handler = FakeSseHandler.HoldingStream(out var gate, out var stream);
        var client = CreateClient(handler);
        var (payloads, _, failures) = Subscribe(client);

        var started = client.Start();
        client.Dispose(); // race: dispose while SendAsync is still pending

        gate.TrySetResult(FakeSseHandler.CreateSseResponse(stream));
        await started.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(client.Connected.IsCanceled);
        Assert.Empty(failures);
        Assert.Empty(payloads);
    }

    [Fact]
    public async Task StartAfterDispose_IsANoop_NotObjectDisposedException()
    {
        var handler = FakeSseHandler.Streaming(out var stream);
        var client = CreateClient(handler);
        client.Dispose();

        // Regression: Start() used to read the disposed CTS token and throw
        // ObjectDisposedException. It must be a safe no-op instead.
        var started = client.Start();

        await started.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(client.Connected.IsCanceled);
    }

    [Fact]
    public async Task Dispose_CompletesAllObservables_WithoutFailure()
    {
        var handler = FakeSseHandler.Streaming(out var stream);
        var client = CreateClient(handler);
        var (payloads, _, failures) = Subscribe(client);
        var payloadsCompleted = false;
        var failuresCompleted = false;
        client.Payloads.Subscribe(_ => { }, () => payloadsCompleted = true);
        client.Failures.Subscribe(_ => { }, () => failuresCompleted = true);

        var started = client.Start();
        stream.Push("event: next\ndata: \"before-dispose\"\n\n");
        await Task.Delay(50); // let the event flow before disposing

        client.Dispose();
        await started.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Single(payloads);
        Assert.Empty(failures);
        Assert.True(payloadsCompleted);
        Assert.True(failuresCompleted);
    }
}
