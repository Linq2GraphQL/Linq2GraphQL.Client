using Linq2GraphQL.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.TestClient;

public static class SampleClientExtensions 
{
    private const string ClientName = "SampleClient";
        
    public static IGraphClientBuilder<SampleClient> AddSampleClient(this IServiceCollection services)
    {
        var graphClientOptions = new GraphClientOptions();
        return GraphClientBuilder(services, graphClientOptions);
    }
    
    public static IGraphClientBuilder<SampleClient> AddSampleClient(this IServiceCollection services, Action<GraphClientOptions> opts)
    {
        var graphClientOptions = new GraphClientOptions();
        opts(graphClientOptions);
        
        return GraphClientBuilder(services, graphClientOptions);
    }

    private static IGraphClientBuilder<SampleClient> GraphClientBuilder(IServiceCollection services,
        GraphClientOptions graphClientOptions)
    {
        var opts = Options.Create(graphClientOptions);
        services.AddKeyedSingleton(opts, ClientName);
        services.AddMemoryCache();        
        return new ClientBuilder<SampleClient>(ClientName, services);
    }
}