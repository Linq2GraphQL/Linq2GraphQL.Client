using Linq2GraphQL.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.TestClientNullable;

public static class SampleNullableClientExtensions 
{
    private const string ClientName = "SampleNullableClient";
        
    public static IGraphClientBuilder<SampleNullableClient> AddSampleNullableClient(this IServiceCollection services)
    {
        var graphClientOptions = new GraphClientOptions();
        return GraphClientBuilder(services, graphClientOptions);
    }
    
    public static IGraphClientBuilder<SampleNullableClient> AddSampleNullableClient(this IServiceCollection services, Action<GraphClientOptions> opts)
    {
        var graphClientOptions = new GraphClientOptions();
        opts(graphClientOptions);
        
        return GraphClientBuilder(services, graphClientOptions);
    }

    private static IGraphClientBuilder<SampleNullableClient> GraphClientBuilder(IServiceCollection services,
        GraphClientOptions graphClientOptions)
    {
        var opts = Options.Create(graphClientOptions);
        services.AddSingleton(opts);
        services.AddMemoryCache();        
        return new ClientBuilder<SampleNullableClient>(ClientName, services);
    }
}