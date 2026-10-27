using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using Shouldly;
using Package = Linq2GraphQL.TestClient;
using Standalone = Linq2GraphQL.TestClientStandalone;

namespace Linq2GraphQL.Tests;

/// <summary>
/// A standalone client (its own copy of the runtime) and a package-based client in one process, sharing
/// an endpoint and the app's IMemoryCache, as an app that uses clients from several teams would.
/// </summary>
public class CoexistenceTests
{
    private static readonly Uri baseAddress = new("https://localhost:7184/graphql/");

    [Fact]
    public async Task StandaloneAndPackageClient_SharingCacheAndEndpoint_BothWork()
    {
        using var application = new WebApplicationFactory<Program>();

        var package = new Package.SampleClient(application.CreateClient(new() { BaseAddress = baseAddress }),
            Options.Create(new Client.GraphClientOptions { UseSafeMode = true }), application.Services);
        var standalone = new Standalone.SampleClient(application.CreateClient(new() { BaseAddress = baseAddress }),
            Options.Create(new Standalone.Runtime.GraphClientOptions { UseSafeMode = true }), application.Services);

        // Safe mode caches the schema per endpoint; each client must get its own runtime's schema type.
        (await package.Query.Customers().Select().ExecuteAsync()).Count.ShouldBe(2);
        (await standalone.Query.Customers().Select().ExecuteAsync()).Count.ShouldBe(2);
        (await package.Query.Hello().Select().ExecuteAsync()).ShouldBe("Hello, World!");
        (await standalone.Query.Hello().Select().ExecuteAsync()).ShouldBe("Hello, World!");
    }

    [Fact]
    public void StandaloneClient_DoesNotReferenceTheRuntimePackages()
    {
        var references = typeof(Standalone.SampleClient).Assembly.GetReferencedAssemblies().Select(e => e.Name);

        references.ShouldNotContain("Linq2GraphQL.Client");
        references.ShouldNotContain("Linq2GraphQL.Client.Subscriptions");
    }
}
