using System.Net.Http.Json;
using System.Text.Json;

namespace Linq2GraphQL.Client;

public class QueryExecutor<T>
{
    private const string errorPropertyName = "errors";
    private const string dataPropertyName = "data";
    private const string extensionsPropertyName = "extensions";
    
    private readonly GraphClient client;

    internal QueryExecutor(GraphClient client)
    {
        this.client = client;
    }

    internal async Task<T> ExecuteRequestAsync(string name, GraphQLRequest graphRequest, CancellationToken cancellationToken = default)
    {
        using var response = await client.HttpClient.PostAsJsonAsync("", graphRequest, client.SerializerOptions, cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GraphQueryRequestException($"Http error! Status code {response.StatusCode} Error: {content}",
                graphRequest.Query, graphRequest.Variables);
        }

        var con = await response.Content.ReadAsStringAsync(cancellationToken);
        return ProcessResponse(con, name, graphRequest);
    }

    public T ProcessResponse(string con, string name, GraphQLRequest request)
    {
        var document = JsonDocument.Parse(con);
        var hasError = document.RootElement.TryGetProperty(errorPropertyName, out var errorElement);
        var hasExtensions = document.RootElement.TryGetProperty(extensionsPropertyName, out var extensionsElement);

        if (hasError)
        {
            var errors = errorElement.Deserialize<List<GraphQueryError>>(client.SerializerOptions);
            throw new GraphQueryExecutionException(errors, request.Query, request.Variables);
        }

        document.RootElement.TryGetProperty(dataPropertyName, out var dataElement);
        dataElement.TryGetProperty(name, out var resultElement);

        if (resultElement.ValueKind == JsonValueKind.Null)
        {
            return default;
        }

        return resultElement.Deserialize<T>(client.SerializerOptions);

    }
}
