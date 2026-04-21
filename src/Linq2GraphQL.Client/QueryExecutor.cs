using System.Net.Http.Json;
using System.Text.Json;

namespace Linq2GraphQL.Client;

public class QueryExecutor<T>
{
    private const string ErrorPropertyName = "errors";
    private const string DataPropertyName = "data";
    private const string ExtensionsPropertyName = "extensions";

    private readonly GraphClient client;

    internal QueryExecutor(GraphClient client)
    {
        this.client = client;
    }

    internal async Task<T> ExecuteRequestAsync(string name, GraphQLRequest graphRequest,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteRawAsync(name, graphRequest, cancellationToken);

        if (result.HasErrors)
        {
            throw new GraphQueryExecutionException(result.Errors, graphRequest.Query, graphRequest.Variables);
        }

        return result.Data;
    }

    internal async Task<GraphResult<T>> ExecuteRawAsync(string name, GraphQLRequest graphRequest,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.HttpClient.PostAsJsonAsync("", graphRequest, client.SerializerOptions,
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GraphQueryRequestException($"Http error! Status code {response.StatusCode} Error: {content}",
                graphRequest.Query, graphRequest.Variables);
        }

        var con = await response.Content.ReadAsStringAsync(cancellationToken);
        return ProcessResponseFull(con, name);
    }

    public T ProcessResponse(string con, string name, GraphQLRequest request)
    {
        var result = ProcessResponseFull(con, name);

        if (result.HasErrors)
        {
            throw new GraphQueryExecutionException(result.Errors, request.Query, request.Variables);
        }

        return result.Data;
    }

    public GraphResult<T> ProcessResponseFull(string con, string name)
    {
        var document = JsonDocument.Parse(con);
        var root = document.RootElement;

        List<GraphQueryError> errors = null;
        if (root.TryGetProperty(ErrorPropertyName, out var errorElement))
        {
            errors = errorElement.Deserialize<List<GraphQueryError>>(client.SerializerOptions);
        }

        T data = default;
        if (root.TryGetProperty(DataPropertyName, out var dataElement) && dataElement.ValueKind != JsonValueKind.Null)
        {
            if (dataElement.TryGetProperty(name, out var resultElement) && resultElement.ValueKind != JsonValueKind.Null)
            {
                data = resultElement.Deserialize<T>(client.SerializerOptions);
            }
        }

        Dictionary<string, object> extensions = null;
        if (root.TryGetProperty(ExtensionsPropertyName, out var extensionsElement))
        {
            extensions = extensionsElement.Deserialize<Dictionary<string, object>>(client.SerializerOptions);
        }

        return new GraphResult<T>
        {
            Data = data,
            Errors = errors,
            Extensions = extensions
        };
    }
}