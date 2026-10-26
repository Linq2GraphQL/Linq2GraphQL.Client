using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using Shouldly;
// An app using several clients imports every runtime namespace; extension methods such as
// ExecuteAsync must still resolve because each one targets its own copy's types.
using Linq2GraphQL.TestClient.Runtime;
using Linq2GraphQL.SecondClient.Runtime;
using Linq2GraphQL.LegacyClient.Runtime;
using Current = Linq2GraphQL.TestClient;
using Second = Linq2GraphQL.SecondClient;
using Legacy = Linq2GraphQL.LegacyClient;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Several standalone clients in one process, sharing one HttpClient base address and one
/// IMemoryCache, as an app that depends on clients from several teams would.
/// </summary>
public class CoexistenceTests
{
    private static readonly Uri baseAddress = new("https://localhost:7184/graphql/");

    [Fact]
    public async Task LegacyAndCurrentClient_InOneProcess_BothWork()
    {
        using var application = new WebApplicationFactory<Program>();

        var legacy = new Legacy.SampleClient(application.CreateClient(new() { BaseAddress = baseAddress }),
            Options.Create(new Legacy.Runtime.GraphClientOptions { UseSafeMode = true }), application.Services);
        var current = new Current.SampleClient(application.CreateClient(new() { BaseAddress = baseAddress }),
            Options.Create(new Current.Runtime.GraphClientOptions { UseSafeMode = true }), application.Services);

        (await legacy.Query.Customers().Select().ExecuteAsync()).Count.ShouldBe(2);
        (await current.Query.Customers().Select().ExecuteAsync()).Count.ShouldBe(2);
        (await legacy.Query.Hello().Select().ExecuteAsync()).ShouldBe("Hello, World!");
        (await current.Query.Hello().Select().ExecuteAsync()).ShouldBe("Hello, World!");
    }

    [Fact]
    public async Task TwoClientsFromTheSameVersion_SharingCacheAndEndpoint_BothWork()
    {
        using var application = new WebApplicationFactory<Program>();

        var current = new Current.SampleClient(application.CreateClient(new() { BaseAddress = baseAddress }),
            Options.Create(new Current.Runtime.GraphClientOptions { UseSafeMode = true }), application.Services);
        var second = new Second.SampleClient(application.CreateClient(new() { BaseAddress = baseAddress }),
            Options.Create(new Second.Runtime.GraphClientOptions { UseSafeMode = true }), application.Services);

        (await current.Query.Customers().Select().ExecuteAsync()).Count.ShouldBe(2);
        (await second.Query.Customers().Select().ExecuteAsync()).Count.ShouldBe(2);
    }
}
