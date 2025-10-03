using Linq2GraphQL.TestServer;
using Linq2GraphQL.TestServer.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddGraphQLServer()
    .ModifyCostOptions(options =>
    {
        options.EnforceCostLimits = false;
    })
    .AddTestServerTypes()
    .AddType<Pig>()
    .AddType<Spider>()
    .AddInMemorySubscriptions()
    .AddFiltering()
    .AddSorting();

builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseWebSockets();

app.MapGraphQL();

app.Run();

public partial class Program { }