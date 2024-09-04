﻿
using Linq2GraphQL.TestServerNullable;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGraphQLServer()
    .AddInMemorySubscriptions()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    //.AddSubscriptionType<Subscription>()
    //.AddType<Pig>()
    //.AddType<Spider>()
    .AddFiltering()
    .AddSorting();

builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseWebSockets();

app.MapGraphQL();

app.Run();

public partial class ProgramNullable { }
