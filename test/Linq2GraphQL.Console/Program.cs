using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole();
});

var serviceProvider = new ServiceCollection();

serviceProvider.AddSingleton(loggerFactory);

serviceProvider
    .AddSampleClient(x =>
    {
        x.UseSafeMode = true;
        x.SubscriptionProtocol = SubscriptionProtocol.ServerSentEvents;
    })
    .WithHttpClient(
        httpClient => { httpClient.BaseAddress = new Uri("https://localhost:7184/graphql"); },
        builder =>
        {
            //Add stuff here
        });

var services = serviceProvider.BuildServiceProvider();

var sampleClient = services.GetRequiredService<SampleClient>();

Console.WriteLine("Goodby, World!");