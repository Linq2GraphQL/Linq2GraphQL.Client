using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;

namespace Linq2GraphQL.Client.Subscriptions;

public class SSEClient : IDisposable
{
    private readonly GraphClient graphClient;
    private readonly GraphQLRequest payload;
    private readonly Subject<string> subscriptionSubject = new();
    private readonly Subject<GraphQueryExecutionException> errorSubject = new();
    private HttpResponseMessage response;
    private StreamReader streamReader;

    public SSEClient(GraphClient graphClient, GraphQLRequest payload)
    {
        this.graphClient = graphClient;
        this.payload = payload;
    }

    public IObservable<string> Subscription => subscriptionSubject.AsObservable();
    public IObservable<GraphQueryExecutionException> Errors => errorSubject.AsObservable();

    public void Dispose()
    {
        streamReader?.Dispose();
        response?.Dispose();
    }

    public async Task Start()
    {
        var json = JsonSerializer.Serialize(payload, graphClient.SerializerOptions);

        var request = new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json)
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        try
        {
            response = await graphClient.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        }
        catch (HttpRequestException ex)
        {
            throw new GraphQueryRequestException(
                $"SSE connection failed: {ex.Message}",
                payload.Query, payload.Variables);
        }

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new GraphQueryRequestException(
                $"SSE connection failed with status {response.StatusCode}: {content}",
                payload.Query, payload.Variables);
        }

        streamReader = new StreamReader(await response.Content.ReadAsStreamAsync());

        while (await streamReader.ReadLineAsync() is { } message)
        {
            if (message.StartsWith("data: "))
            {
                var jsonData = message.Substring(6);
                subscriptionSubject.OnNext(jsonData);
            }
            else if (message.StartsWith("event: error"))
            {
                var errorData = await streamReader.ReadLineAsync();
                if (errorData != null && errorData.StartsWith("data: "))
                {
                    var errorJson = errorData.Substring(6);
                    var errors = new List<GraphQueryError> { new() { Message = errorJson } };
                    errorSubject.OnNext(new GraphQueryExecutionException(errors, payload.Query, payload.Variables));
                }
            }
        }
    }
}