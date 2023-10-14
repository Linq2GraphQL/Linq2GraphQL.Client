using System.Text.Json.Serialization;

namespace Linq2GraphQL.Client;

public class GraphRequest
{
    [JsonPropertyName("query")] public string Query { get; set; }

    [JsonPropertyName("variables")] public Dictionary<string, object> Variables { get; set; }
}