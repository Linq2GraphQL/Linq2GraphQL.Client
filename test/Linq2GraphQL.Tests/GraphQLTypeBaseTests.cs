using System.Text.Json;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.Tests;

public class GraphQLTypeBaseTests
{
    [Fact]
    public void AdditionalProperties_NullByDefault()
    {
        var instance = new TestType();

        Assert.Null(instance.__AdditionalProperties);
    }

    [Fact]
    public void UnknownProperty_IsStillCaptured()
    {
        var instance = JsonSerializer.Deserialize<TestType>("""{"foo":123}""", ClientJson.Default);

        Assert.NotNull(instance);
        Assert.Equal(123, instance.__AdditionalProperties["foo"].GetInt32());
    }

    [Fact]
    public void GetMethodValue_MissingKey_ReturnsDefault()
    {
        var instance = new TestType();

        Assert.Null(instance.GetMethodValue<string>("missingMethod"));
        Assert.Equal(0, instance.GetMethodValue<int>("missingMethod"));
    }

    [Fact]
    public void GetMethodValue_DecodesCapturedValue()
    {
        // No arguments → GetArgumentsId returns null → key is the bare method name.
        var instance = new TestType();
        instance.__AdditionalProperties = new Dictionary<string, JsonElement>
        {
            ["orders"] = JsonSerializer.SerializeToElement("captured-value")
        };

        Assert.Equal("captured-value", instance.GetMethodValue<string>("orders"));
    }

    [Fact]
    public void GetMethodValue_ArgumentsChangeKey()
    {
        var instance = new TestType();
        instance.__AdditionalProperties = new Dictionary<string, JsonElement>
        {
            [$"orders{Utilities.GetArgumentsId(new object[] { 5 })}"] = JsonSerializer.SerializeToElement(55)
        };

        Assert.Equal(55, instance.GetMethodValue<int>("orders", 5));
        Assert.Equal(0, instance.GetMethodValue<int>("orders", 6));
    }

    private sealed class TestType : GraphQLTypeBase { }
}
