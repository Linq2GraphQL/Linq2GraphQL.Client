using System.Text.Json.Serialization;

namespace Linq2GraphQL.Client.Subscriptions;

public class WebsocketRequest
{
    public WebsocketRequest(string type)
    {
        Type = type;
    }

    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("payload")] public GraphRequest Payload { get; set; }
}

//public class WebsocketRequestPayload
//{
//    [JsonPropertyName("query")]
//    public string Query { get; set; }

//    [JsonPropertyName("variables")]
//    public Dictionary<string, object> Variables { get; set; }
//}