using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarWars.Client;

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

serviceProvider.AddStarWarsClient(x =>
{
    x.UseSafeMode = true;
    x.SubscriptionProtocol = SubscriptionProtocol.ServerSentEvents;
})
   .WithHttpClient(
       httpClient => { httpClient.BaseAddress = new Uri("https://swapi-graphql.netlify.app/.netlify/functions/index"); });


var services = serviceProvider.BuildServiceProvider();
var sampleClient = services.GetRequiredService<SampleClient>();
var starWarsClient = services.GetRequiredService<StarWarsClient>();

var films = await starWarsClient
            .Query
            .AllFilms(null, 2, null, null)
            .Include(e => e.Films.Select(e => e.VehicleConnection(null, 2, null, null)))
            .Select(e=> e.Films)
            .ExecuteAsync();

Console.WriteLine("Goodby, World!");