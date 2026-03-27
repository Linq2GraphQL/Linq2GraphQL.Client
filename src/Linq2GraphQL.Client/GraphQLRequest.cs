using System.Text.Json.Serialization;

namespace Linq2GraphQL.Client;

public record GraphQLRequest
{
    [JsonPropertyName("query")] public string Query { get; init; }

    [JsonPropertyName("variables")] public Dictionary<string, object> Variables { get; init; }
}