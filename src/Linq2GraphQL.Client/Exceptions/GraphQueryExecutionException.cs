using System.Text.Json.Serialization;

namespace Linq2GraphQL.Client;

public class GraphQueryExecutionException : Exception
{
    public GraphQueryExecutionException(string query) : base("Unexpected error response received from server.")
    {
        GraphQLQuery = query;
    }

    public GraphQueryExecutionException(IEnumerable<GraphQueryError> errors, string query)
        : base($"{errors.FirstOrDefault()?.Message} - Check {nameof(Errors)} property for details")
    {
        Errors = errors;
        GraphQLQuery = query;
    }

    public string GraphQLQuery { get; private set; }
    public IEnumerable<GraphQueryError> Errors { get; private set; }
}

public class GraphQueryError
{
    [JsonPropertyName("message")] public string Message { get; set; }

    [JsonPropertyName("locations")] public ErrorLocation[] Locations { get; set; }

    [JsonPropertyName("path")] public List<string> Path { get; set; }
}

public class ErrorLocation
{
    [JsonPropertyName("line")] public int Line { get; set; }

    [JsonPropertyName("column")] public int Column { get; set; }
}