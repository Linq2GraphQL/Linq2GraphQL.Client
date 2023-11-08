using System.Net.Http.Json;
using System.Text.Json;

namespace Linq2GraphQL.Client;

public class QueryExecutor<T>
{
    private const string errorPathPropertyName = "errors";
    private const string dataPathPropertyName = "data";
    private readonly GraphClient client;

    internal QueryExecutor(GraphClient client)
    {
        this.client = client;
    }

    internal async Task<T> ExecuteRequestAsync(string alias, GraphQLRequest graphRequest)
    {
        var json = JsonSerializer.Serialize(graphRequest, client.SerializerOptions);
        using var response = await client.HttpClient.PostAsJsonAsync("", graphRequest, client.SerializerOptions);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new GraphQueryRequestException($"Http error! Status code {response.StatusCode} Error: {content}",
                graphRequest.Query);
        }

        var con = await response.Content.ReadAsStringAsync();
        return ProcessResponse(con, alias, graphRequest.Query);
    }

    public T ProcessResponse(string con, string alias, string query)
    {
        var document = JsonDocument.Parse(con);
        var hasError = document.RootElement.TryGetProperty(errorPathPropertyName, out var errorElement);

        if (hasError)
        {
            var errors = errorElement.Deserialize<List<GraphQueryError>>(client.SerializerOptions);
            throw new GraphQueryExecutionException(errors, query);
        }

        var hasData = document.RootElement.TryGetProperty(dataPathPropertyName, out var dataElement);
        var hasResult = dataElement.TryGetProperty(alias, out var resultElement);

        if (resultElement.ValueKind == JsonValueKind.Null)
        {
            return default;
        }

        return resultElement.Deserialize<T>(client.SerializerOptions);

    }
}
