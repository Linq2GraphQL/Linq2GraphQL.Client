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
        using var request = new HttpRequestMessage(HttpMethod.Post, "");
        request.Content = JsonContent.Create(graphRequest, options: client.SerializerOptions);
        await client.ConfigureRequestAsync(request, cancellationToken);

        // With ResponseHeadersRead, HttpClient.Timeout only covers the time until the
        // headers arrive. Extend it over reading the body with a linked token source.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (client.HttpClient.Timeout != Timeout.InfiniteTimeSpan)
        {
            timeoutCts.CancelAfter(client.HttpClient.Timeout);
        }

        try
        {
            using var response = await client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                content = Truncate(content, MaxErrorMessageLength);
                throw new GraphQueryRequestException($"Http error! Status code {response.StatusCode} Error: {content}",
                    graphRequest.Query, graphRequest.Variables);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, timeoutCts.Token);

            // Parse in place — GetBuffer avoids copying the body a second time.
            return Common.GraphEnvelopeReader.Read<T>(
                memory.GetBuffer().AsSpan(0, (int)memory.Length), name, client.SerializerOptions);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Only the timeout fired — surface it the way HttpClient does.
            throw new TaskCanceledException("The request timed out.", new TimeoutException());
        }
    }

    private const int MaxErrorMessageLength = 4096;

    private static string Truncate(string value, int maxLength)
    {
        if (value == null || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength) + "...(truncated)";
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

    /// <summary>
    ///     Parses a response from an already-materialized <see cref="JsonElement"/> root
    ///     (the full envelope) and throws <see cref="GraphQueryExecutionException"/> on errors.
    /// </summary>
    public T ProcessResponse(JsonElement root, string name, GraphQLRequest request)
    {
        var result = ProcessResponseFull(root, name);

        if (result.HasErrors)
        {
            throw new GraphQueryExecutionException(result.Errors, request.Query, request.Variables);
        }

        return result.Data;
    }

    /// <summary>
    ///     Parses a response envelope from raw UTF-8 bytes (used by the SSE byte path):
    ///     only the requested root field is deserialized — no JsonDocument and no UTF-16
    ///     round trip, matching the query path.
    /// </summary>
    public T ProcessResponse(byte[] utf8Json, string name, GraphQLRequest request)
    {
        var result = Common.GraphEnvelopeReader.Read<T>(utf8Json, name, client.SerializerOptions);

        if (result.HasErrors)
        {
            throw new GraphQueryExecutionException(result.Errors, request.Query, request.Variables);
        }

        return result.Data;
    }

    /// <summary>
    ///     Full-envelope parse from raw UTF-8 bytes (SSE/WS byte path). Returns data and
    ///     errors together and never throws on GraphQL <c>errors</c>.
    /// </summary>
    public GraphResult<T> ProcessResponseFull(byte[] utf8Json, string name)
    {
        return Common.GraphEnvelopeReader.Read<T>(utf8Json, name, client.SerializerOptions);
    }
    public GraphResult<T> ProcessResponseFull(string con, string name)
    {
        using var document = JsonDocument.Parse(con);
        return ProcessResponseFull(document.RootElement, name);
    }

    public GraphResult<T> ProcessResponseFull(JsonElement root, string name)
    {
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
