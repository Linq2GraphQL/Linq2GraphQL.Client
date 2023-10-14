using Linq2GraphQL.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.TestClientNullable;

public class SampleNullableClient
{
    public SampleNullableClient(HttpClient httpClient, IOptions<GraphClientOptions> options, IServiceProvider provider)
    {
        var client = new GraphClient(httpClient, options, provider);
        Query = new QueryMethods(client);
    }

    public QueryMethods Query { get; private set; }
    
}