using System.Text;
using System.Text.Json;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Linq2GraphQL.Tests;

/// <summary>
///     Direct tests of GraphEnvelopeReader (the span-based envelope parser used by the
///     streaming query path and the SSE byte path) and of the public QueryExecutor
///     ProcessResponse overloads built on top of it.
/// </summary>
public class EnvelopeAndResponseTests
{
    private static GraphResult<T> Read<T>(string json, string name = "customer")
    {
        return GraphEnvelopeReader.Read<T>(Encoding.UTF8.GetBytes(json), name, ClientJson.Default);
    }

    [Fact]
    public void EmptyBody_ReturnsDefaultResult_NoErrors()
    {
        var result = Read<ProbeCustomer>("", "customer");

        Assert.Null(result.Data);
        Assert.Null(result.Errors);
        Assert.Null(result.Extensions);
    }

    [Fact]
    public void NonObjectRoot_ThrowsJsonException()
    {
        Assert.ThrowsAny<JsonException>(() => Read<ProbeCustomer>("[1,2,3]"));
        Assert.ThrowsAny<JsonException>(() => Read<ProbeCustomer>("42"));
        Assert.ThrowsAny<JsonException>(() => Read<ProbeCustomer>("null"));
    }

    [Fact]
    public void DataScalar_ThrowsJsonException()
    {
        Assert.ThrowsAny<JsonException>(() => Read<ProbeCustomer>("""{"data": 42}"""));
    }

    [Fact]
    public void TargetField_IsDeserialized_SiblingsAreSkipped()
    {
        var result = Read<ProbeCustomer>("""{"data":{"other":42,"nested":{"deep":[1,2]},"customer":{"name":"n"}}}""");

        Assert.Equal("n", result.Data?.name);
        Assert.Null(result.Errors);
    }

    [Fact]
    public void ErrorsBeforeData_BothAreRead()
    {
        var result = Read<ProbeCustomer>(
            """{"errors":[{"message":"e1"}],"data":{"customer":{"name":"n"}}}""");

        Assert.Equal("n", result.Data?.name);
        Assert.Equal("e1", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void Extensions_AreDeserialized_ValuesAreJsonElements()
    {
        var result = Read<ProbeCustomer>("""{"data":{"customer":{"name":"n"}},"extensions":{"trace":"abc","n":5}}""");

        Assert.NotNull(result.Extensions);
        Assert.Equal(2, result.Extensions.Count);
        Assert.IsType<JsonElement>(result.Extensions["trace"]);
        Assert.Equal("abc", ((JsonElement)result.Extensions["trace"]).GetString());
        Assert.Equal(5, ((JsonElement)result.Extensions["n"]).GetInt32());
    }

    [Fact]
    public void NullTargetField_ReturnsDefaultData()
    {
        var result = Read<ProbeCustomer>("""{"data":{"customer":null}}""");

        Assert.Null(result.Data);
        Assert.Null(result.Errors);
    }

    // ---- QueryExecutor.ProcessResponse public overloads ----

    private static QueryExecutor<ProbeCustomer> CreateExecutor()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("http://test.local/graphql/") };
        var graphClient = new GraphClient(httpClient,
            Microsoft.Extensions.Options.Options.Create(new GraphClientOptions()), new ServiceCollection().BuildServiceProvider());
        return new QueryExecutor<ProbeCustomer>(graphClient);
    }

    [Fact]
    public void ProcessResponse_ByteOverload_ReadsOnlyRequestedRootField()
    {
        var executor = CreateExecutor();
        var bytes = Encoding.UTF8.GetBytes("""{"data":{"other":1,"customer":{"name":"byte-path"}}}""");
        var request = new GraphQLRequest { Query = "query { customer { name } }" };

        var data = executor.ProcessResponse(bytes, "customer", request);

        Assert.Equal("byte-path", data.name);
    }

    [Fact]
    public async Task ProcessResponse_ByteOverload_ErrorsThrow()
    {
        var executor = CreateExecutor();
        var bytes = Encoding.UTF8.GetBytes("""{"errors":[{"message":"boom"}],"data":null}""");
        var request = new GraphQLRequest { Query = "query { customer { name } }" };

        var exception = await Assert.ThrowsAsync<GraphQueryExecutionException>(() =>
            Task.FromResult(executor.ProcessResponse(bytes, "customer", request)));

        Assert.Equal("boom", Assert.Single(exception.Errors).Message);
    }

    [Fact]
    public void ProcessResponse_JsonElementOverload_ReturnsData()
    {
        var executor = CreateExecutor();
        using var document = JsonDocument.Parse("""{"data":{"customer":{"name":"element-path"}},"junk":true}""");
        var request = new GraphQLRequest { Query = "query { customer { name } }" };

        var data = executor.ProcessResponse(document.RootElement.Clone(), "customer", request);

        Assert.Equal("element-path", data.name);
    }

    [Fact]
    public void ProcessResponseFull_JsonElementOverload_ReturnsErrorsAndExtensions()
    {
        var executor = CreateExecutor();
        using var document = JsonDocument.Parse(
            """{"errors":[{"message":"err"}],"data":null,"extensions":{"code":"X"}}""");

        var result = executor.ProcessResponseFull(document.RootElement.Clone(), "customer");

        Assert.Equal("err", Assert.Single(result.Errors).Message);
        Assert.NotNull(result.Extensions);
    }

    public sealed class ProbeCustomer
    {
        public string name { get; set; } = null!;
    }
}
