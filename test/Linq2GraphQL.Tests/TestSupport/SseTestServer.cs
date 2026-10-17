using System.Net;
using System.Text;
using Linq2GraphQL.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.Tests;

/// <summary>
///     A Stream whose reads pull from a scriptable queue of chunks. Test code pushes
///     SSE frames (or raw bytes) at will; reads block while the queue is empty, so the
///     SSEClient parser loop parks exactly like it would against a live socket.
/// </summary>
internal sealed class ScriptableSseStream : Stream
{
    private readonly object gate = new();
    private readonly Queue<byte[]> chunks = new();
    private TaskCompletionSource<bool> available = NewSignal();
    private byte[]? current;
    private int currentOffset;
    private bool ended;
    private int parkedReads;

    /// <summary>
    ///     How many times a read has parked on an empty queue. Once it moves past a value
    ///     sampled before a push, the reader has consumed that push and issued its next read
    ///     (so a wrapping IdleTimeoutStream has re-armed its timer).
    /// </summary>
    public int ParkedReads => Volatile.Read(ref parkedReads);

    private static TaskCompletionSource<bool> NewSignal()
    {
        return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Queues a UTF-8 chunk (e.g. one or more SSE frames) and releases pending reads.</summary>
    public void Push(string text)
    {
        Push(Encoding.UTF8.GetBytes(text));
    }

    public void Push(byte[] bytes)
    {
        TaskCompletionSource<bool> signal;
        lock (gate)
        {
            chunks.Enqueue(bytes);
            signal = available;
            available = NewSignal();
        }

        signal.TrySetResult(true);
    }

    /// <summary>Marks the end of the stream: the next read when idle returns 0 (EOF).</summary>
    public void CompleteStream()
    {
        TaskCompletionSource<bool> signal;
        lock (gate)
        {
            ended = true;
            signal = available;
            available = NewSignal();
        }

        signal.TrySetResult(true);
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            TaskCompletionSource<bool> wait;
            lock (gate)
            {
                if (current is null && chunks.Count > 0)
                {
                    current = chunks.Dequeue();
                    currentOffset = 0;
                }

                if (current is not null)
                {
                    var copy = Math.Min(count, current.Length - currentOffset);
                    Array.Copy(current, currentOffset, buffer, offset, copy);
                    currentOffset += copy;
                    if (currentOffset == current.Length)
                    {
                        current = null;
                    }

                    return copy;
                }

                if (ended)
                {
                    return 0;
                }

                wait = available;
                Interlocked.Increment(ref parkedReads);
            }

            await wait.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

/// <summary>
///     HttpMessageHandler with scriptable SSE responses: a live event stream, a fixed
///     status/content type, or a held response released by the test (dispose races).
/// </summary>
internal sealed class FakeSseHandler : HttpMessageHandler
{
    private readonly Func<CancellationToken, Task<HttpResponseMessage>> responder;
    public HttpRequestMessage? LastRequest { get; private set; }
    public int RequestCount;

    private FakeSseHandler(Func<CancellationToken, Task<HttpResponseMessage>> responder)
    {
        this.responder = responder;
    }

    /// <summary>A 200 text/event-stream response whose body is the scriptable stream.</summary>
    public static FakeSseHandler Streaming(out ScriptableSseStream stream)
    {
        var body = new ScriptableSseStream();
        stream = body;
        return new FakeSseHandler(_ => Task.FromResult(CreateSseResponse(body)));
    }

    /// <summary>A response with a non-success status code (and optional content type/body).</summary>
    public static FakeSseHandler Status(HttpStatusCode statusCode, string contentType = "application/json",
        string body = "nope")
    {
        return new FakeSseHandler(_ => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, contentType)
        }));
    }

    /// <summary>SendAsync blocks until the test completes the returned gate with a response.</summary>
    public static FakeSseHandler Holding(out TaskCompletionSource<HttpResponseMessage> gate)
    {
        var source = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        gate = source;
        return new FakeSseHandler(_ => source.Task);
    }

    /// <summary>A ready-to-release gate; test completes it with a response of its choosing.</summary>
    public static FakeSseHandler HoldingStream(out TaskCompletionSource<HttpResponseMessage> gate,
        out ScriptableSseStream stream)
    {
        var body = new ScriptableSseStream();
        stream = body;
        var source = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        gate = source;
        return new FakeSseHandler(_ => source.Task);
    }

    internal static HttpResponseMessage CreateSseResponse(ScriptableSseStream body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(body)
            {
                Headers = { ContentType = new("text/event-stream") }
            }
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref RequestCount);
        LastRequest = request;
        return responder(cancellationToken);
    }
}

internal static class TestGraphClientFactory
{
    /// <summary>A GraphClient over the handler with safe mode OFF (no introspection, no provider).</summary>
    public static GraphClient Create(HttpMessageHandler handler, GraphClientOptions? options = null)
    {
        options ??= new GraphClientOptions { SubscriptionProtocol = SubscriptionProtocol.ServerSentEvents };
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test.local/graphql/") };
        return new GraphClient(httpClient, Options.Create(options), new ServiceCollection().BuildServiceProvider());
    }
}
