using Linq2GraphQL.Docs;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TabBlazor;
using StarWars.Client;
using Linq2GraphQL.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddTabler();

builder.Services.AddStarWarsClient(x =>
 {
     x.UseSafeMode = false;
     x.SubscriptionProtocol = SubscriptionProtocol.ServerSentEvents;
 })
   .WithHttpClient(
       httpClient => { httpClient.BaseAddress = new Uri("https://swapi-graphql.netlify.app/.netlify/functions/index"); });

await builder.Build().RunAsync();
