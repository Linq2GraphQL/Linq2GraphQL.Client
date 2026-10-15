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
        using var request = new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = JsonContent.Create(graphRequest, options: client.SerializerOptions)
        };
        await client.ConfigureRequestAsync(request, cancellationToken);

        using var response = await client.HttpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GraphQueryRequestException($"Http error! Status code {response.StatusCode} Error: {content}",
                graphRequest.Query, graphRequest.Variables);
        }

        var con = await response.Content.ReadAsStringAsync(cancellationToken);
        return ProcessResponseFull(con, name);
    }

    /// <summary>
    /// Sends a request and returns the whole <c>data</c> object as JSON, for queries written as text rather
    /// than built from expressions.
    /// </summary>
    internal async Task<GraphResult<JsonElement>> ExecuteDocumentAsync(GraphQLRequest graphRequest,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = JsonContent.Create(graphRequest, options: client.SerializerOptions)
        };
        await client.ConfigureRequestAsync(request, cancellationToken);

        using var response = await client.HttpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode && !IsGraphQLResponse(content))
        {
            throw new GraphQueryRequestException($"Http error! Status code {response.StatusCode} Error: {content}",
                graphRequest.Query, graphRequest.Variables);
        }

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        return new GraphResult<JsonElement>
        {
            Data = root.TryGetProperty(DataPropertyName, out var data) ? data.Clone() : default,
            Errors = root.TryGetProperty(ErrorPropertyName, out var errors)
                ? errors.Deserialize<List<GraphQueryError>>(client.SerializerOptions)
                : null,
            Extensions = root.TryGetProperty(ExtensionsPropertyName, out var extensions)
                ? extensions.Deserialize<Dictionary<string, object>>(client.SerializerOptions)
                : null
        };
    }

    /// <summary>
    /// GraphQL servers answer a request they reject (for example a validation error) with a non-success
    /// status but a regular GraphQL body; that body is a result, not a transport failure.
    /// </summary>
    private static bool IsGraphQLResponse(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                   (document.RootElement.TryGetProperty(ErrorPropertyName, out _) ||
                    document.RootElement.TryGetProperty(DataPropertyName, out _));
        }
        catch (JsonException)
        {
            return false;
        }
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