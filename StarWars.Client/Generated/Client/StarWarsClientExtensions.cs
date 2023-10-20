using Linq2GraphQL.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StarWars.Client;

public static class StarWarsClientExtensions 
{
    private const string ClientName = "StarWarsClient";
        
    public static IGraphClientBuilder<StarWarsClient> AddStarWarsClient(this IServiceCollection services)
    {
        var graphClientOptions = new GraphClientOptions();
        return GraphClientBuilder(services, graphClientOptions);
    }
    
    public static IGraphClientBuilder<StarWarsClient> AddStarWarsClient(this IServiceCollection services, Action<GraphClientOptions> opts)
    {
        var graphClientOptions = new GraphClientOptions();
        opts(graphClientOptions);
        
        return GraphClientBuilder(services, graphClientOptions);
    }

    private static IGraphClientBuilder<StarWarsClient> GraphClientBuilder(IServiceCollection services,
        GraphClientOptions graphClientOptions)
    {
        var opts = Options.Create(graphClientOptions);
        services.AddSingleton(opts);
        services.AddMemoryCache();        
        return new ClientBuilder<StarWarsClient>(ClientName, services);
    }
}