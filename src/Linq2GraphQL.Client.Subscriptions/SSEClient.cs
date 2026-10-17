using System.Net.Http.Headers;
using System.Net.ServerSentEvents;
using System.Net.Mime;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;

namespace Linq2GraphQL.Client.Subscriptions;

public class SSEClient : IDisposable
{
    private readonly GraphClient graphClient;
    private readonly GraphQLRequest payload;
    private readonly ISubject<byte[]> payloadSubject = Subject.Synchronize(new Subject<byte[]>());
    private readonly ISubject<GraphQueryExecutionException> errorSubject =
        Subject.Synchronize(new Subject<GraphQueryExecutionException>());
    private readonly ISubject<Exception> failureSubject = Subject.Synchronize(new Subject<Exception>());
    private readonly CancellationTokenSource cancellationSource = new();
    private readonly object resourceLock = new();
    private readonly TaskCompletionSource<object> connectedTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private HttpResponseMessage response;
    private Stream eventStream;
    private int disposed;
    private int failureReported;
    private int completedSent;

    public SSEClient(GraphClient graphClient, GraphQLRequest payload)
    {
        this.graphClient = graphClient;
        this.payload = payload;
    }

    /// <summary>Clock for the idle timeout; tests substitute a fake provider.</summary>
    internal TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>
    ///     UTF-8 payloads of "next" events decoded to strings — deferred per subscriber:
    ///     the decode only happens for listeners actually attached here.
    /// </summary>
    public IObservable<string> Subscription => payloadSubject
        .Select(static data => Encoding.UTF8.GetString(data));

    /// <summary>
    ///     Completes when the server has accepted the SSE POST (status + content type
    ///     verified); faults/cancels if the connection fails or is disposed pre-connect.
    /// </summary>
    internal Task Connected => connectedTcs.Task;

    public IObservable<GraphQueryExecutionException> Errors => errorSubject.AsObservable();
    internal IObservable<Exception> Failures => failureSubject.AsObservable();

    /// <summary>
    ///     Raw UTF-8 payloads of "next" events — the byte-level feed used by
    ///     GraphSubscriptionExecute to avoid a string round trip per message.
    /// </summary>
    internal IObservable<byte[]> Payloads => payloadSubject.AsObservable();

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        try
        {
            cancellationSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The reader has already completed and disposed the token source.
        }

        lock (resourceLock)
        {
            eventStream?.Dispose();
            response?.Dispose();
        }
        payloadSubject.OnCompleted();
        errorSubject.OnCompleted();
        failureSubject.OnCompleted();
        cancellationSource.Dispose();
    }

    public Task Start()
    {
        if (Volatile.Read(ref disposed) != 0)
        {
            connectedTcs.TrySetCanceled();
            return Task.CompletedTask;
        }

        return Start(cancellationSource.Token);
    }

    internal async Task Start(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref disposed) != 0)
        {
            // Disposed before start: a no-op, not an ObjectDisposedException.
            connectedTcs.TrySetCanceled();
            return;
        }

        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationSource.Token, cancellationToken);
        var token = linkedSource.Token;

        using var request = new HttpRequestMessage(HttpMethod.Post, "");

        try
        {
            var json = JsonSerializer.Serialize(payload, graphClient.SerializerOptions);
            request.Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            await graphClient.ConfigureRequestAsync(request, token).ConfigureAwait(false);
            var sentResponse = await graphClient.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token)
                .ConfigureAwait(false);
            lock (resourceLock)
            {
                response = sentResponse;
                if (Volatile.Read(ref disposed) != 0)
                {
                    response.Dispose();
                    response = null;
                    connectedTcs.TrySetCanceled();
                    return;
                }
            }

            if (!sentResponse.IsSuccessStatusCode)
            {
                var content = await sentResponse.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                throw new GraphQueryRequestException(
                    $"SSE connection failed with status {sentResponse.StatusCode}: {content}",
                    payload.Query, payload.Variables);
            }

            if (!string.Equals(sentResponse.Content.Headers.ContentType?.MediaType, "text/event-stream",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new GraphQueryRequestException(
                    $"SSE connection returned unsupported content type '{sentResponse.Content.Headers.ContentType}'.",
                    payload.Query, payload.Variables);
            }

            var rawStream = await sentResponse.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            Stream source = rawStream;

            // The server has accepted the subscription POST — the transport is live.
            connectedTcs.TrySetResult(null);

            // Optional idle timeout: any bytes (including ':' keep-alive comments, which
            // the SSE parser consumes invisibly) reset the timer.
            var idleTimeout = graphClient.ClientOptions?.SubscriptionIdleTimeout;
            if (idleTimeout is { } timeout)
            {
                source = new IdleTimeoutStream(rawStream, timeout, TimeProvider);
            }

            lock (resourceLock)
            {
                eventStream = source;
                if (Volatile.Read(ref disposed) != 0)
                {
                    eventStream.Dispose();
                    eventStream = null;
                    connectedTcs.TrySetCanceled();
                    return;
                }
            }

            // SseParser (System.Net.ServerSentEvents) implements the WHATWG framing:
            // multi-line data joined with '\n', comments ignored, dispatch on blank line,
            // incomplete trailing event discarded at EOF. Data is kept as UTF-8 bytes.
            var parser = SseParser.Create(source, static (_, data) => data.ToArray());
            await foreach (var item in parser.EnumerateAsync(token).ConfigureAwait(false))
            {
                // Older GraphQL-over-SSE servers omit `event: next`. SseParser reports the
                // SSE default event type "message" when no event field was sent — map both
                // to "next"; Dispatch ignores unknown types (keep-alives etc.) otherwise.
                var eventType = item.EventType;
                if (string.IsNullOrEmpty(eventType) || eventType == "message")
                {
                    eventType = "next";
                }

                var sseEvent = new SseEvent(eventType, item.Data);

                if (Dispatch(sseEvent))
                {
                    return;
                }
            }

            token.ThrowIfCancellationRequested();
            throw new IOException("SSE stream ended before a complete event was received.");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Disposal/unsubscription is the normal way to stop an open stream.
            connectedTcs.TrySetCanceled();
        }
        catch (Exception exception)
        {
            ReportFailure(exception is HttpRequestException or IOException or OperationCanceledException
                ? new GraphQueryRequestException($"SSE connection failed: {exception.Message}",
                    payload.Query, payload.Variables)
                : exception);
        }
        finally
        {
            lock (resourceLock)
            {
                response?.Dispose();
                response = null;
                eventStream?.Dispose();
                eventStream = null;
            }

            // Complete only if neither a failure nor an explicit server complete
            // already terminated the stream (prevents a double OnCompleted).
            if (Volatile.Read(ref failureReported) == 0
                && Volatile.Read(ref completedSent) == 0
                && Volatile.Read(ref disposed) == 0)
            {
                payloadSubject.OnCompleted();
            }
        }
    }

    private bool Dispatch(SseEvent message)
    {
        switch (message.EventType)
        {
            case "next":
                payloadSubject.OnNext(message.Data);
                return false;
            case "error":
                ReportFailure(ParseSseError(message.Data));
                return true;
            case "complete":
                // Guard against a duplicate complete.
                if (Interlocked.Exchange(ref completedSent, 1) == 0)
                {
                    payloadSubject.OnCompleted();
                }

                return true;
            default:
                // WHATWG SSE spec: unknown event names dispatch to no listener. Servers
                // and proxies legitimately add event types (e.g. HotChocolate "ka"
                // keep-alives); ignoring them keeps the stream alive.
                return false;
        }
    }

    private Exception ParseSseError(byte[] json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var errors = root.ValueKind == JsonValueKind.Array
                ? root.Deserialize<List<GraphQueryError>>(graphClient.SerializerOptions)
                : root.TryGetProperty("errors", out var errorsElement)
                    ? errorsElement.Deserialize<List<GraphQueryError>>(graphClient.SerializerOptions)
                    : new List<GraphQueryError> { new() { Message = Encoding.UTF8.GetString(json) } };
            return new GraphQueryExecutionException(errors, payload.Query, payload.Variables);
        }
        catch (JsonException)
        {
            return new GraphQueryExecutionException(
                new List<GraphQueryError> { new() { Message = Encoding.UTF8.GetString(json) } },
                payload.Query, payload.Variables);
        }
        catch (InvalidOperationException)
        {
            return new GraphQueryExecutionException(
                new List<GraphQueryError> { new() { Message = Encoding.UTF8.GetString(json) } },
                payload.Query, payload.Variables);
        }
    }

    private void ReportFailure(Exception exception)
    {
        // Idempotent: only the first failure emits. This uses a dedicated flag so a
        // later Dispose() still runs (it owns resource cleanup via the disposed flag).
        if (Interlocked.Exchange(ref failureReported, 1) != 0)
        {
            return;
        }

        if (exception is GraphQueryExecutionException executionException)
        {
            errorSubject.OnNext(executionException);
        }

        // WhenConnected waiters learn about connect failures; a no-op once connected.
        connectedTcs.TrySetException(exception);

        failureSubject.OnNext(exception);
        failureSubject.OnCompleted();
        payloadSubject.OnCompleted();
    }
}

internal readonly record struct SseEvent(string EventType, byte[] Data);
