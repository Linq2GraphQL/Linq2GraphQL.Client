namespace Linq2GraphQL.Client;

/// <summary>
///     What happens when the subscription event buffer (SubscriptionMaxBufferedEvents)
///     overflows because the observer cannot keep up.
/// </summary>
public enum SubscriptionOverflowPolicy
{
    /// <summary>Fault the subscription with an InvalidOperationException (default - never drops events silently).</summary>
    Fault = 0,

    /// <summary>Drop the oldest queued events to make room; the newest events are always delivered.</summary>
    DropOldest,

    /// <summary>Keep only the most recent event; observers that only need the latest value.</summary>
    KeepLatest,
}

public class GraphClientOptions
{
    public bool UseSafeMode { get; set; } = false;
    public SubscriptionProtocol SubscriptionProtocol { get; set; } = default;
    public Func<GraphClient, Task<GraphQLRequest>> WSConnectionInitPayload { get; set; } =
        _ => Task.FromResult<GraphQLRequest>(null!);

    /// <summary>
    /// Called for every HTTP request the client sends (queries, mutations, the safe-mode schema query and
    /// server-sent-event subscriptions) just before it is sent. Use it to set per-request headers, such as an
    /// access token, without a <see cref="DelegatingHandler"/>.
    /// </summary>
    public Func<HttpRequestMessage, CancellationToken, ValueTask> ConfigureRequest { get; set; }

    /// <summary>
    /// Separates the safe-mode schema cache entry of this client from other clients that use the same base
    /// address. Set it when such clients see different schemas, for example because they use different
    /// credentials.
    /// </summary>
    public string SchemaCacheKey { get; set; }

    /// <summary>
    ///     Maximum number of subscription events buffered between the transport and the
    ///     observer. Overflow behavior is controlled by SubscriptionOverflowPolicy.
    /// </summary>
    public int SubscriptionMaxBufferedEvents { get; set; } = 128;

    /// <summary>Behavior when the subscription event buffer overflows.</summary>
    public SubscriptionOverflowPolicy SubscriptionOverflowPolicy { get; set; } = SubscriptionOverflowPolicy.Fault;

    /// <summary>
    ///     Optional idle timeout for SSE subscriptions. If no bytes (including keep-alive
    ///     comments) arrive within this window, the subscription fails. Null disables it.
    /// </summary>
    public TimeSpan? SubscriptionIdleTimeout { get; set; }

    /// <summary>
    ///     Maximum number of WebSocket reconnect attempts after a dropped connection before
    ///     the subscription fails with an error. Attempts use exponential backoff with jitter
    ///     (graphql-ws default: 2^n seconds plus 300-3000 ms). A successful connection resets
    ///     the count. Fatal close codes never retry. Default 5; 0 disables reconnection.
    /// </summary>
    public int SubscriptionRetryAttempts { get; set; } = 5;
}
