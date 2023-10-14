using Microsoft.Extensions.DependencyInjection;

namespace Linq2GraphQL.Client;

public interface IGraphClientBuilder<T> where T : class
{
    public string ClientName { get; }
    public IServiceCollection Services { get; }
}