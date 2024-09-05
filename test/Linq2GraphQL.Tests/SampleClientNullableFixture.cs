using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Linq2GraphQL.TestClientNullable;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.Tests;

public class SampleNullableClientFixture : IDisposable
{
    internal readonly SampleNullableClient sampleClient;

    public SampleNullableClientFixture()
    {
        var baseAddress = new Uri("https://localhost:50741/graphql/");

        var application = new WebApplicationFactory<ProgramNullable>();
        var client = application.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = baseAddress });

        sampleClient = new SampleNullableClient(client, Options.Create(new GraphClientOptions
        {
            SubscriptionProtocol = SubscriptionProtocol.ServerSentEvents,
            UseSafeMode = false,
        }), application.Services);
        //Please note currently only ServerSentEvents work in test project
    }

    public void Dispose() { }
}