using System.Text.Json;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client.Converters;
using Linq2GraphQL.Client.Schema;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.Client;

public class GraphClient
{
    private readonly IMemoryCache cache;
    private readonly IOptions<GraphClientOptions> options;
    private readonly bool includeDeprecated;

    /// <summary>
    /// Constructor for backward compatibility with code compiled against 0.13.83 and earlier.
    /// This constructor calls the 4-parameter constructor with includeDeprecated = false.
    /// </summary>
    public GraphClient(HttpClient httpClient, IOptions<GraphClientOptions> options, IServiceProvider provider)
        : this(httpClient, options, provider, false)
    {
    }

    /// <summary>
    /// Constructor with optional includeDeprecated parameter.
    /// </summary>
    public GraphClient(HttpClient httpClient, IOptions<GraphClientOptions> options, IServiceProvider provider,
        bool includeDeprecated = false)
    {
        this.options = options;
        this.includeDeprecated = includeDeprecated;
        if (options.Value.UseSafeMode)
        {
            cache = provider.GetRequiredService<IMemoryCache>();
        }

        HttpClient = httpClient;

        SerializerOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, Converters = { }, };

        SubscriptionUrl = GetSubscriptionUrl();
    }

    private GraphClient(GraphClient source, Func<HttpRequestMessage, CancellationToken, ValueTask> configureRequest)
    {
        options = source.options;
        includeDeprecated = source.includeDeprecated;
        cache = source.cache;
        HttpClient = source.HttpClient;
        SerializerOptions = source.SerializerOptions;
        SubscriptionUrl = source.SubscriptionUrl;
        requestConfigurations = [..source.requestConfigurations, configureRequest];
    }

    /// <summary>
    /// Hooks added with <see cref="WithRequestConfiguration"/>, run in order after
    /// <see cref="GraphClientOptions.ConfigureRequest"/>.
    /// </summary>
    private readonly Func<HttpRequestMessage, CancellationToken, ValueTask>[] requestConfigurations = [];

    /// <summary>
    /// A client that shares this client's HttpClient, options, schema cache and serializer settings, and also
    /// runs <paramref name="configureRequest"/> on every request it sends, after the hooks already in place.
    /// Use it for per-user or per-conversation headers such as an access token. Creating one is cheap, and
    /// this client is not changed.
    /// </summary>
    public GraphClient WithRequestConfiguration(Func<HttpRequestMessage, CancellationToken, ValueTask> configureRequest)
    {
        ArgumentNullException.ThrowIfNull(configureRequest);
        return new GraphClient(this, configureRequest);
    }

    /// <summary>
    /// Executes a query written as GraphQL text and returns the whole <c>data</c> object as JSON, together
    /// with any errors and extensions. GraphQL errors, including a server rejecting the document, are
    /// returned in the result rather than thrown; only transport failures throw
    /// <see cref="GraphQueryRequestException"/>. The request goes through the same hooks as typed queries.
    /// </summary>
    public Task<GraphResult<JsonElement>> ExecuteAsync(string query, IDictionary<string, object> variables = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var request = new GraphQLRequest
        {
            Query = query,
            Variables = variables == null ? null : new Dictionary<string, object>(variables)
        };
        return new QueryExecutor<JsonElement>(this).ExecuteDocumentAsync(request, cancellationToken);
    }

    public string SubscriptionUrl { get; }
    public SubscriptionProtocol SubscriptionProtocol => options.Value.SubscriptionProtocol;
    public HttpClient HttpClient { get; }
    public JsonSerializerOptions SerializerOptions { get; }


    public Func<GraphClient, Task<GraphQLRequest>> WSConnectionInitPayload => options.Value.WSConnectionInitPayload;

    internal async ValueTask ConfigureRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (options.Value.ConfigureRequest is { } configureRequest)
        {
            await configureRequest(request, cancellationToken);
        }

        foreach (var requestConfiguration in requestConfigurations)
        {
            await requestConfiguration(request, cancellationToken);
        }
    }

    private string GetSubscriptionUrl()
    {
        var baseUrl = HttpClient?.BaseAddress.ToString();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentNullException("HttpClient BaseAddress must be set");
        }

        if (SubscriptionProtocol == SubscriptionProtocol.ServerSentEvents)
        {
            return baseUrl;
        }

        return HttpClient.BaseAddress.Scheme switch
        {
            "http" => new UriBuilder(HttpClient.BaseAddress) { Scheme = "ws" }.Uri.ToString(),
            "https" => new UriBuilder(HttpClient.BaseAddress) { Scheme = "wss" }.Uri.ToString(),
            _ => throw new Exception($"BaseAddress Scheme {HttpClient.BaseAddress.Scheme} is unknown")
        };
    }

    public async Task<GraphQLSchema> GetSchemaForSafeModeAsync()
    {
        if (!options.Value.UseSafeMode)
        {
            return null;
        }

        var cacheKey = $"Linq2GraphQL_Schema:{HttpClient.BaseAddress}:{includeDeprecated}:{options.Value.SchemaCacheKey}";

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            var executor = new QueryExecutor<GraphQLSchema>(this);

            string query;
            if (includeDeprecated)
            {
                query = Helpers.SchemaQueryIncludeDeprecated;
            }
            else
            {
                query = Helpers.SchemaQuery;
            }

            var graphRequest = new GraphQLRequest { Query = query };
            return await executor.ExecuteRequestAsync("__schema", graphRequest);
        });
    }
}