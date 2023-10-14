using Microsoft.Extensions.DependencyInjection;

namespace Linq2GraphQL.Client;

public class ClientBuilder<T> : IGraphClientBuilder<T> where T : class
{
    public ClientBuilder(
        string clientName,
        IServiceCollection services)
    {
        ClientName = clientName;
        Services = services;
    }

    public string ClientName { get; }
    public IServiceCollection Services { get; }
}