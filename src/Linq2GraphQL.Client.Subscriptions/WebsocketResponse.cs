using System.Text.Json.Serialization;

namespace Linq2GraphQL.Client.Subscriptions;

public class WebsocketResponse
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("payload")] public object Payload { get; set; }
}