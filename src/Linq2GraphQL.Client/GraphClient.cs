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
   
    public GraphClient(HttpClient httpClient, IOptions<GraphClientOptions> options, IServiceProvider provider)
    {
        this.options = options;
        if (options.Value.UseSafeMode)
        {
            cache = provider.GetRequiredService<IMemoryCache>();
        }

        HttpClient = httpClient;

        SerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { },
        };

        SubscriptionUrl = GetSubscriptionUrl();
    }

    public string SubscriptionUrl { get; }
    public SubscriptionProtocol SubscriptionProtocol => options.Value.SubscriptionProtocol;
    public HttpClient HttpClient { get; }
    public JsonSerializerOptions SerializerOptions { get; }
 

    public Func<GraphClient, Task<GraphQLRequest>> WSConnectionInitPayload => options.Value.WSConnectionInitPayload;
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

        var cackeKey = "Linq2GraphQL_Schema:" + HttpClient.BaseAddress;

        return await cache.GetOrCreateAsync(cackeKey, async entry =>
        {
            var executor = new QueryExecutor<GraphQLSchema>(this);

            var graphRequest = new GraphQLRequest { Query = Helpers.SchemaQuery };
            return await executor.ExecuteRequestAsync("__schema", graphRequest);
        });
    }
}