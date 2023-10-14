using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;

namespace Linq2GraphQL.Client.Subscriptions;

public class SSEClient : IDisposable
{
    private readonly GraphClient graphClient;
    private readonly GraphRequest payload;
    private readonly Subject<string> subscriptionSubject = new();
    private HttpResponseMessage response;
    private StreamReader streamReader;

    public SSEClient(GraphClient graphClient, GraphRequest payload)
    {
        this.graphClient = graphClient;
        this.payload = payload;
    }

    public IObservable<string> Subscription => subscriptionSubject.AsObservable();

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
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        response = await graphClient.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        streamReader = new StreamReader(await response.Content.ReadAsStreamAsync());

        while (!streamReader.EndOfStream)
        {
            var message = await streamReader.ReadLineAsync();

            if (message.StartsWith("data: "))
            {
                var jsonData = message.Substring(6);
                subscriptionSubject.OnNext(jsonData);
            }
        }
    }
}