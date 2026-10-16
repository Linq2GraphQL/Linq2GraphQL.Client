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
    public void ProcessResponse_StringOverload_ErrorsThrowAndPreserveMetadata()
    {
        var executor = CreateExecutor();
        var variables = new Dictionary<string, object> { ["id"] = 7 };
        var request = new GraphQLRequest { Query = "query { customer { name } }", Variables = variables };

        var exception = Assert.Throws<GraphQueryExecutionException>(() =>
            executor.ProcessResponse("""{"errors":[{"message":"string-path failure"}]}""",
                "customer", request));

        Assert.Equal("string-path failure", Assert.Single(exception.Errors).Message);
        Assert.Equal(request.Query, exception.GraphQLQuery);
        Assert.Same(variables, exception.GraphQLVariables);
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
    public void ProcessResponse_JsonElementOverload_ErrorsThrowAndPreserveMetadata()
    {
        var executor = CreateExecutor();
        var variables = new Dictionary<string, object> { ["id"] = 7 };
        var request = new GraphQLRequest { Query = "query { customer { name } }", Variables = variables };
        using var document = JsonDocument.Parse("""{"errors":[{"message":"element-path failure"}]}""");

        var exception = Assert.Throws<GraphQueryExecutionException>(() =>
            executor.ProcessResponse(document.RootElement.Clone(), "customer", request));

        Assert.Equal("element-path failure", Assert.Single(exception.Errors).Message);
        Assert.Equal(request.Query, exception.GraphQLQuery);
        Assert.Same(variables, exception.GraphQLVariables);
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

    [Fact]
    public void ProcessResponseFull_StringOverload_ReturnsDataErrorsAndExtensions()
    {
        var executor = CreateExecutor();

        var result = executor.ProcessResponseFull(
            """{"data":{"customer":{"name":"partial"}},"errors":[{"message":"field failure"}],"extensions":{"trace":"x"}}""",
            "customer");

        Assert.Equal("partial", result.Data.name);
        Assert.Equal("field failure", Assert.Single(result.Errors).Message);
        Assert.Equal("x", ((JsonElement)result.Extensions["trace"]).GetString());
    }

    // ---- Single parser: every public overload agrees with GraphEnvelopeReader ----

    public static TheoryData<string> Envelopes => new()
    {
        """{"data":{"customer":{"name":"n"}}}""",
        """{"data":{"other":42,"customer":{"name":"n"},"tail":[1,{"x":2}]}}""",
        """{"errors":[{"message":"e1"}],"data":{"customer":{"name":"partial"}},"extensions":{"trace":"t"}}""",
        """{"data":{"customer":{"name":"n"}},"errors":[{"message":"e2","path":["customer"]}]}""",
        """{"data":{"customer":null}}""",
        """{"data":null,"errors":[{"message":"boom"}]}""",
        """{"data":{}}""",
        """{"extensions":{"cost":1},"junk":{"deep":[1,2,3]}}""",
        """{ "data" : { "customer" : { "name" : "spaced \u00e9" } } }""",
    };

    [Theory]
    [MemberData(nameof(Envelopes))]
    public void AllOverloads_ProduceTheSameResult(string json)
    {
        var executor = CreateExecutor();
        var expected = Read<ProbeCustomer>(json);

        var fromString = executor.ProcessResponseFull(json, "customer");
        var fromBytes = executor.ProcessResponseFull(Encoding.UTF8.GetBytes(json), "customer");
        using var document = JsonDocument.Parse(json);
        var fromElement = executor.ProcessResponseFull(document.RootElement, "customer");

        foreach (var actual in new[] { fromString, fromBytes, fromElement })
        {
            Assert.Equal(expected.Data?.name, actual.Data?.name);
            Assert.Equal(expected.Errors?.Select(e => e.Message), actual.Errors?.Select(e => e.Message));
            Assert.Equal(expected.Extensions?.Keys.OrderBy(k => k), actual.Extensions?.Keys.OrderBy(k => k));
        }
    }

    [Fact]
    public void ProcessResponseFull_EmptyString_ReturnsNoDataInsteadOfThrowing()
    {
        var executor = CreateExecutor();

        var result = executor.ProcessResponseFull("", "customer");

        Assert.Null(result.Data);
        Assert.Null(result.Errors);
        Assert.False(result.HasData);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void ProcessResponse_EmptyString_ReturnsDefault()
    {
        var executor = CreateExecutor();

        var data = executor.ProcessResponse("", "customer", new GraphQLRequest { Query = "query { customer }" });

        Assert.Null(data);
    }

    [Fact]
    public void ProcessResponseFull_NullString_Throws()
    {
        var executor = CreateExecutor();

        Assert.Throws<ArgumentNullException>(() => executor.ProcessResponseFull((string)null!, "customer"));
    }

    [Fact]
    public void ProcessResponseFull_JsonElementOverload_NonObjectRoot_ThrowsJsonException()
    {
        var executor = CreateExecutor();
        using var document = JsonDocument.Parse("[1,2]");

        Assert.ThrowsAny<JsonException>(() => executor.ProcessResponseFull(document.RootElement, "customer"));
        Assert.ThrowsAny<JsonException>(() => executor.ProcessResponseFull(default(JsonElement), "customer"));
    }

    [Fact]
    public void ProcessResponseFull_JsonElementOverload_SubElementIsReadOnItsOwn()
    {
        // A JsonElement that is a member of a larger document: only its own raw text is parsed.
        var executor = CreateExecutor();
        using var document = JsonDocument.Parse(
            """{"wrapper":{"payload":{"data":{"customer":{"name":"inner"}}},"data":{"customer":{"name":"outer"}}}}""");

        var payload = document.RootElement.GetProperty("wrapper").GetProperty("payload");
        var result = executor.ProcessResponseFull(payload, "customer");

        Assert.Equal("inner", result.Data?.name);
    }

    // ---- ReadDocument: whole data object for queries written as GraphQL text ----

    [Fact]
    public void ReadDocument_ReturnsWholeDataObject_AndOutlivesTheBuffer()
    {
        var bytes = Encoding.UTF8.GetBytes(
            """{"data":{"a":1,"b":{"c":"x"}},"errors":[{"message":"m"}],"extensions":{"k":true}}""");

        var result = GraphEnvelopeReader.ReadDocument(bytes, ClientJson.Default, out var isEnvelope);
        Array.Clear(bytes);

        Assert.True(isEnvelope);
        Assert.Equal(JsonValueKind.Object, result.Data.ValueKind);
        Assert.Equal(1, result.Data.GetProperty("a").GetInt32());
        Assert.Equal("x", result.Data.GetProperty("b").GetProperty("c").GetString());
        Assert.Equal("m", Assert.Single(result.Errors).Message);
        Assert.True(((JsonElement)result.Extensions["k"]).GetBoolean());
    }

    [Theory]
    [InlineData("""{"data":null}""", true, JsonValueKind.Null)]
    [InlineData("""{"errors":[{"message":"m"}]}""", true, JsonValueKind.Undefined)]
    [InlineData("""{"message":"not graphql"}""", false, JsonValueKind.Undefined)]
    [InlineData("", false, JsonValueKind.Undefined)]
    public void ReadDocument_ReportsWhetherTheBodyIsAGraphQLEnvelope(string json, bool expectedEnvelope,
        JsonValueKind expectedData)
    {
        var result = GraphEnvelopeReader.ReadDocument(Encoding.UTF8.GetBytes(json), ClientJson.Default,
            out var isEnvelope);

        Assert.Equal(expectedEnvelope, isEnvelope);
        Assert.Equal(expectedData, result.Data.ValueKind);
    }

    public sealed class ProbeCustomer
    {
        public string name { get; set; } = null!;
    }
}
