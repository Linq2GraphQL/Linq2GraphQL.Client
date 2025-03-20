using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.Tests;

public class SampleClientFixture : IDisposable
{
    internal readonly SampleClient sampleClient;

    public SampleClientFixture()
    {
        var baseAddress = new Uri("https://localhost:7184/graphql/");

        var application = new WebApplicationFactory<Program>();
        var client = application.CreateClient(new() { BaseAddress = baseAddress });

        sampleClient = new(client,
            Options.Create(new GraphClientOptions
            {
                SubscriptionProtocol = SubscriptionProtocol.ServerSentEvents, UseSafeMode = true
            }), application.Services);
        //Please note currently only ServerSentEvents work in test project
    }

    public void Dispose()
    {
    }
}