using System.Text.Json;
using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Shouldly;

namespace Linq2GraphQL.Tests;

public class ErrorHandlingTests : IClassFixture<SampleClientFixture>
{
    private readonly SampleClient sampleClient;

    public ErrorHandlingTests(SampleClientFixture fixture)
    {
        sampleClient = fixture.sampleClient;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // === GraphQueryError deserialization tests ===

    [Fact]
    public void GraphQueryError_Deserialize_MessageOnly()
    {
        var json = @"{""message"":""Something went wrong""}";
        var error = JsonSerializer.Deserialize<GraphQueryError>(json, JsonOptions);

        error.ShouldNotBeNull();
        error.Message.ShouldBe("Something went wrong");
        error.ErrorCode.ShouldBe(GraphErrorCode.Unknown);
    }

    [Fact]
    public void GraphQueryError_Deserialize_WithExtensions()
    {
        var json = @"{""message"":""Not authenticated"",""extensions"":{""code"":""UNAUTHENTICATED"",""statusCode"":401}}";
        var error = JsonSerializer.Deserialize<GraphQueryError>(json, JsonOptions);

        error.ShouldNotBeNull();
        error.Message.ShouldBe("Not authenticated");
        error.Extensions.ShouldNotBeNull();
        error.Extensions["code"].ShouldNotBeNull();
        error.ErrorCode.ShouldBe(GraphErrorCode.Authentication);
    }

    [Fact]
    public void GraphQueryError_Deserialize_WithLocations()
    {
        var json = @"{""message"":""Syntax Error"",""locations"":[{""line"":3,""column"":10}]}";
        var error = JsonSerializer.Deserialize<GraphQueryError>(json, JsonOptions);

        error.ShouldNotBeNull();
        error.Locations.ShouldNotBeNull();
        error.Locations.Length.ShouldBe(1);
        error.Locations[0].Line.ShouldBe(3);
        error.Locations[0].Column.ShouldBe(10);
    }

    [Fact]
    public void GraphQueryError_Deserialize_WithPath()
    {
        var json = @"{""message"":""fail"",""path"":[""createUser"",""email""]}";
        var error = JsonSerializer.Deserialize<GraphQueryError>(json, JsonOptions);

        error.ShouldNotBeNull();
        error.Path.ShouldNotBeNull();
        error.Path.Count.ShouldBe(2);
    }

    [Fact]
    public void GraphQueryError_Deserialize_FullError()
    {
        var json = @"{""message"":""Validation failed"",""locations"":[{""line"":3,""column"":10}],""path"":[""createUser"",""email""],""extensions"":{""code"":""BAD_USER_INPUT"",""statusCode"":400}}";
        var error = JsonSerializer.Deserialize<GraphQueryError>(json, JsonOptions);

        error.ShouldNotBeNull();
        error.Message.ShouldBe("Validation failed");
        error.Locations.ShouldNotBeNull();
        error.Locations.Length.ShouldBe(1);
        error.Path.ShouldNotBeNull();
        error.Path.Count.ShouldBe(2);
        error.Extensions.ShouldNotBeNull();
        error.ErrorCode.ShouldBe(GraphErrorCode.BadRequest);
    }

    // === GraphQL response parsing tests ===

    [Fact]
    public void GraphQlResponse_ErrorsOnly_ParsedCorrectly()
    {
        var json = @"{""errors"":[{""message"":""Something went wrong"",""extensions"":{""code"":""UNAUTHENTICATED""}}]}";
        var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("errors", out var errorsEl).ShouldBeTrue();
        var errors = errorsEl.Deserialize<List<GraphQueryError>>(JsonOptions);
        errors.ShouldNotBeNull();
        errors.Count.ShouldBe(1);
        errors[0].Message.ShouldBe("Something went wrong");
        errors[0].ErrorCode.ShouldBe(GraphErrorCode.Authentication);
    }

    [Fact]
    public void GraphQlResponse_DataAndErrors_BothPresent()
    {
        var json = @"{""data"":{""hello"":""partial""},""errors"":[{""message"":""field x failed""}]}";
        var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("data", out var dataEl).ShouldBeTrue();
        doc.RootElement.TryGetProperty("errors", out var errorsEl).ShouldBeTrue();

        dataEl.TryGetProperty("hello", out var helloEl).ShouldBeTrue();
        helloEl.GetString().ShouldBe("partial");

        var errors = errorsEl.Deserialize<List<GraphQueryError>>(JsonOptions);
        errors!.Count.ShouldBe(1);
        errors[0].Message.ShouldBe("field x failed");
    }

    [Fact]
    public void GraphQlResponse_ResponseExtensions_Parsed()
    {
        var json = @"{""data"":{""test"":""hello""},""extensions"":{""trackingId"":""abc-123"",""timing"":42.5}}";
        var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("extensions", out var extEl).ShouldBeTrue();
        var extensions = extEl.Deserialize<Dictionary<string, object>>(JsonOptions);
        extensions.ShouldNotBeNull();
        extensions.ContainsKey("trackingId").ShouldBeTrue();
    }

    // === Error code classification tests ===

    [Theory]
    [InlineData("UNAUTHENTICATED", GraphErrorCode.Authentication)]
    [InlineData("FORBIDDEN", GraphErrorCode.Forbidden)]
    [InlineData("BAD_USER_INPUT", GraphErrorCode.BadRequest)]
    [InlineData("INTERNAL_SERVER_ERROR", GraphErrorCode.InternalServerError)]
    [InlineData("GRAPHQL_VALIDATION_FAILED", GraphErrorCode.Validation)]
    [InlineData("PERSISTED_QUERY_NOT_FOUND", GraphErrorCode.PersistedQueryNotFound)]
    [InlineData("RATE_LIMITED", GraphErrorCode.RateLimited)]
    [InlineData("TIMEOUT", GraphErrorCode.Timeout)]
    [InlineData("NOT_FOUND", GraphErrorCode.NotFound)]
    [InlineData("CONFLICT", GraphErrorCode.Conflict)]
    [InlineData("UNKNOWN_THING", GraphErrorCode.Unknown)]
    public void GraphErrorCode_ClassifiesCodes(string code, GraphErrorCode expected)
    {
        var error = new GraphQueryError
        {
            Message = "test",
            Extensions = new Dictionary<string, object> { { "code", code } }
        };
        error.ErrorCode.ShouldBe(expected);
    }

    [Fact]
    public void GraphErrorCode_NoExtensions_ReturnsUnknown()
    {
        var error = new GraphQueryError { Message = "test" };
        error.ErrorCode.ShouldBe(GraphErrorCode.Unknown);
    }

    [Fact]
    public void GraphErrorCode_NoCodeInExtensions_ReturnsUnknown()
    {
        var error = new GraphQueryError
        {
            Message = "test",
            Extensions = new Dictionary<string, object> { { "other", "value" } }
        };
        error.ErrorCode.ShouldBe(GraphErrorCode.Unknown);
    }

    // === GraphResult<T> tests ===

    [Fact]
    public void GraphResult_EnsureNoErrors_ThrowsWhenHasErrors()
    {
        var result = new GraphResult<string?>
        {
            Data = null,
            Errors = new List<GraphQueryError> { new() { Message = "fail" } },
            Extensions = null
        };

        Assert.Throws<GraphQueryExecutionException>(() => result.EnsureNoErrors());
    }

    [Fact]
    public void GraphResult_EnsureNoErrors_DoesNotThrowWhenNoErrors()
    {
        var result = new GraphResult<string>
        {
            Data = "hello",
            Errors = null,
            Extensions = null
        };

        result.EnsureNoErrors();
    }

    [Fact]
    public void GraphResult_HasErrors_NullErrors_ReturnsFalse()
    {
        var result = new GraphResult<string> { Errors = null };
        result.HasErrors.ShouldBeFalse();
    }

    [Fact]
    public void GraphResult_HasErrors_EmptyErrors_ReturnsFalse()
    {
        var result = new GraphResult<string> { Errors = new List<GraphQueryError>() };
        result.HasErrors.ShouldBeFalse();
    }

    [Fact]
    public void GraphResult_HasData_NonDefaultData_ReturnsTrue()
    {
        var result = new GraphResult<string> { Data = "hello" };
        result.HasData.ShouldBeTrue();
    }

    [Fact]
    public void GraphResult_HasData_NullData_ReturnsFalse()
    {
        var result = new GraphResult<string?> { Data = null };
        result.HasData.ShouldBeFalse();
    }

    // === Exception tests ===

    [Fact]
    public void GraphQueryExecutionException_PreservesQueryAndVariables()
    {
        var errors = new List<GraphQueryError>
        {
            new()
            {
                Message = "test error",
                Extensions = new Dictionary<string, object> { { "code", "VALIDATION_FAILED" } }
            }
        };
        var vars = new Dictionary<string, object> { { "id", 42 } };

        var ex = new GraphQueryExecutionException(errors, "query { hello }", vars);

        ex.GraphQLQuery.ShouldBe("query { hello }");
        ex.GraphQLVariables["id"].ShouldBe(42);
        ex.Errors.First().Message.ShouldBe("test error");
        ex.Errors.First().ErrorCode.ShouldBe(GraphErrorCode.Validation);
    }

    [Fact]
    public void GraphQueryExecutionException_NullQueryAndVariables()
    {
        var errors = new List<GraphQueryError> { new() { Message = "fail" } };
        var ex = new GraphQueryExecutionException(errors, null, null);

        ex.Errors.Count().ShouldBe(1);
        ex.GraphQLQuery.ShouldBeNull();
        ex.GraphQLVariables.ShouldBeNull();
    }

    [Fact]
    public void GraphQueryRequestException_PreservesQueryAndVariables()
    {
        var vars = new Dictionary<string, object> { { "name", "test" } };
        var ex = new GraphQueryRequestException("HTTP 500", "query { foo }", vars);

        ex.Message.ShouldContain("HTTP 500");
        ex.GraphQLQuery.ShouldBe("query { foo }");
        ex.GraphQLVariables["name"].ShouldBe("test");
    }

    // === Integration: ExecuteWithResultAsync success path ===

    [Fact]
    public async Task ExecuteWithResultAsync_SuccessfulQuery_ReturnsDataNoErrors()
    {
        var result = await sampleClient
            .Query
            .Hello("World")
            .Select()
            .ExecuteWithResultAsync();

        result.HasErrors.ShouldBeFalse();
        result.HasData.ShouldBeTrue();
        result.Data.ShouldBe("Hello, World!");
        result.Errors.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulQuery_ReturnsData()
    {
        var result = await sampleClient
            .Query
            .Hello("Test")
            .Select()
            .ExecuteAsync();

        result.ShouldBe("Hello, Test!");
    }
}
