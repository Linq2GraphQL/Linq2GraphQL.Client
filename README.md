
<div align='center'>

<img src=https://raw.githubusercontent.com/Linq2GraphQL/Linq2GraphQL.Client/main/Logo.svg alt="logo" width=100 height=100 />

<h1>Linq2GraphQL.Client</h1>
<p>A straightforward Linq to GraphQL Client</p>

<h4> <a href="https://linq2graphql.com"> Documentation </a> <span> · </span> <a href="https://github.com/Linq2GraphQL/Linq2GraphQL.Client/issues"> Report Bug </a> <span> · </span> <a href="https://github.com/Linq2GraphQL/Linq2GraphQL.Client/issues"> Request Feature </a> </h4>

[![Build](https://github.com/Linq2GraphQL/Linq2GraphQL.Client/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Linq2GraphQL/Linq2GraphQL.Client/actions/workflows/ci.yml)

</div>

# Introduction
Linq2GraphQL generates C# classes from the GraphQL schema and and togheter with the nuget package Linq2GraphQL.Client  it makes it possible to query the server using Linq expressions. 

A simple query that will get the first 10 orders with the primitive properties of orders and the connected customer.
```cs
var orders = await sampleClient
    .Query
        .Orders(first: 10)
        .Include(e => e.Orders.Select(e => e.Customer))
        .Select(e => e.Orders)
        .ExecuteAsync();
```

A example mutation where we add a new customer and return the Customer Id.
```cs
 var customerId = await sampleClient
     .Mutation
     .AddCustomer(new CustomerInput
     {
         CustomerId = Guid.NewGuid(),
         CustomerName = "New Customer",
         Status = CustomerStatus.Active
     })
     .Select(e=> e.CustomerId)
     .ExecuteAsync();
```     

# Getting Started
## Generate Client code
There are two options to generate the client code from the GraphQL schema.
Use the online tool to <a href="https://linq2graphql.com/generate-client"> generate</a> or install Linq2GraphQL.Generator as a tool.

Install/Update Tool: 

    dotnet tool update Linq2GraphQL.Generator -g --prerelease
   
   Usage:
       
      Linq2GraphQL.Generator <endpoint> [options]
    
    Arguments:
      <endpoint>  Endpoint of the GraphQL service
    
    Options:
      -o, --output <output>        Output folder, relative to current location [default: Linq2GraphQL_Generated]
      -n, --namespace <namespace>  Namespace of generated classes [default: YourNamespace]
      -c, --client <client>        Name of the generated client [default: GraphQLClient]
      -t, --token <token>          Bearertoken for authentication
      -s, --subscriptions          Include subscriptions (Exprimental)
      -es --enum-strategy          If AddUnknownOption all enums will have an additional Unknown option
      -nu --nullabel               Nullable client [default: false]
	  -d  --deprecated			   Include Deprecated as Obsolete
	
As an example:

    Linq2GraphQL https://spacex-production.up.railway.app/ -c="SpaceXClient" -n="SpaceX" -o="Generated"

Would generate a client from url *https://spacex-production.up.railway.app/* with the name *SpaceXClient* in the namespace *SpaceX* to folder *Generated*

## Add Nuget

Latest stable: [![Nuget](https://img.shields.io/nuget/v/Linq2GraphQL.Client.svg)](https://www.nuget.org/packages/Linq2GraphQL.Client)
<br/>
Latest prerelease: [![Nuget](https://img.shields.io/nuget/vpre/Linq2GraphQL.Client.svg)](https://www.nuget.org/packages/Linq2GraphQL.Client)

    dotnet add package Linq2GraphQL.Client --prerelease

## Dependency Injection
The client adds a set of extensions to make it easier to add the client to dependency injection.
As an example this would add SpaceXClient to the container:
```cs
services
    .SpaceXClient(x =>
     {
         x.UseSafeMode = false;
     })
    .WithHttpClient(
        httpClient => 
        { 
            httpClient.BaseAddress = new Uri("https://spacex-production.up.railway.app/"); 
        });
```
## Safe Mode
Turning on *SafeMode* will make the client before the first request to do an introspection query to the endpoint. The schema will be used to make sure that any auto included properties are available. This is an advanced feature that require the endpoint to support introspection. By default safe mode is turned of.

# Acknowledgments
Linq2GraphQL is inspired by [GraphQLinq](https://github.com/Giorgi/GraphQLinq) , thank you [Giorgi](https://github.com/Giorgi)

[![Stargazers repo roster for @linq2graphql/linq2graphql.client](https://reporoster.com/stars/dark/linq2graphql/linq2graphql.client)](https://github.com/linq2graphql/linq2graphql.client/stargazers)



