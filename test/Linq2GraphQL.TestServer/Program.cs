using Linq2GraphQL.TestServer;
using Linq2GraphQL.TestServer.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGraphQLServer()
    .AddInMemorySubscriptions()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()
    .AddType<Pig>()
    .AddType<Spider>()
    .AddFiltering()
    .AddSorting();

builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseWebSockets();

app.MapGraphQL();

app.Run();

public partial class Program { }