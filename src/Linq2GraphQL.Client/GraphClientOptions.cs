namespace Linq2GraphQL.Client;

public class GraphClientOptions
{
    public bool UseSafeMode { get; set; } = false;
    public SubscriptionProtocol SubscriptionProtocol { get; set; } = default;
    public Func<GraphClient, Task<GraphQLRequest>> WSConnectionInitPayload { get; set; } = opts => null;

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
}
