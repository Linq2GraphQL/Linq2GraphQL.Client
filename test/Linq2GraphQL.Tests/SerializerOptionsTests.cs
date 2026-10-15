using System.Text.Json;
using Linq2GraphQL.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Linq2GraphQL.Tests;

public class SerializerOptionsTests
{
    [Fact]
    public void ClientsSharingOptions_UseTheSameFrozenInstance()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new GraphClientOptions());
        var client1 = new GraphClient(new HttpClient { BaseAddress = new Uri("https://localhost/graphql/") },
            options, new ServiceCollection().BuildServiceProvider());
        var client2 = new GraphClient(new HttpClient { BaseAddress = new Uri("https://localhost/graphql/") },
            options, new ServiceCollection().BuildServiceProvider());

        Assert.NotSame(client1, client2);
        Assert.Same(client1.SerializerOptions, client2.SerializerOptions);
    }

    [Fact]
    public void SharedOptions_Serialize_GraphQLRequest_Compactly()
    {
        // Golden serialization: same wire shape as the historical per-client options.
        var options = Microsoft.Extensions.Options.Options.Create(new GraphClientOptions());
        var client = new GraphClient(new HttpClient { BaseAddress = new Uri("https://localhost/graphql/") },
            options, new ServiceCollection().BuildServiceProvider());

        var request = new GraphQLRequest { Query = "query { hello }" };
        var json = JsonSerializer.Serialize(request, client.SerializerOptions);

        Assert.Equal("""{"query":"query { hello }"}""", json);
    }

    [Fact]
    public void SharedOptions_AreFrozen_MutationThrows()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new GraphClientOptions());
        var client = new GraphClient(new HttpClient { BaseAddress = new Uri("https://localhost/graphql/") },
            options, new ServiceCollection().BuildServiceProvider());
        var serializerOptions = client.SerializerOptions;

        Assert.Throws<InvalidOperationException>(() => serializerOptions.AllowTrailingCommas = true);
        Assert.Throws<InvalidOperationException>(() => serializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));
    }
}
