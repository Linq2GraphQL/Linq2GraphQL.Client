using Linq2GraphQL.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.StarWars;

public class StarWarsClient
{
    public StarWarsClient(HttpClient httpClient, IOptions<GraphClientOptions> options, IServiceProvider provider)
    {
        var client = new GraphClient(httpClient, options, provider);
        Query = new RootMethods(client);
    }

    public RootMethods Query { get; private set; }
    
}