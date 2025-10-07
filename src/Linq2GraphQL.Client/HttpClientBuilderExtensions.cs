using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace Linq2GraphQL.Client;

public static class HttpClientBuilderExtensions
{
    private const string userAgentName = "Linq2GraphQL";
    private static readonly string userAgentVersion = ThisAssembly.AssemblyFileVersion;

    public static IHttpClientBuilder WithHttpClient<T>(
        this IGraphClientBuilder<T> clientBuilder,
        Action<HttpClient> configureClient,
        Action<IHttpClientBuilder> configureClientBuilder = null) where T : class
    {
        var builder = clientBuilder.Services
            .AddHttpClient<T>(
                clientBuilder.ClientName,
                client =>
                {
                    client.DefaultRequestHeaders.UserAgent.Add(
                        new ProductInfoHeaderValue(
                            new ProductHeaderValue(
                                userAgentName,
                                userAgentVersion)));
                    configureClient(client);
                });

        configureClientBuilder?.Invoke(builder);

        return builder;
    }
    
    public static IHttpClientBuilder WithHttpClient<T>(
        this IGraphClientBuilder<T> clientBuilder,
        string httpClientName,
        Action<HttpClient> configureClient,
        Action<IHttpClientBuilder> configureClientBuilder = null) where T : class
    {
        var builder = clientBuilder.Services
            .AddHttpClient<T>(
                httpClientName,
                client =>
                {
                    client.DefaultRequestHeaders.UserAgent.Add(
                        new ProductInfoHeaderValue(
                            new ProductHeaderValue(
                                userAgentName,
                                userAgentVersion)));
                    configureClient(client);
                });

        configureClientBuilder?.Invoke(builder);

        return builder;
    }
}