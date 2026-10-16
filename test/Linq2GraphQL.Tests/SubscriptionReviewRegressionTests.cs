using System.Net;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Text.Json;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Subscriptions;
using Linq2GraphQL.TestClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.Tests;

/// <summary>
///     Regression tests for review findings: typed WebSocket completion, per-subscriber
///     buffer isolation, and fatal-vs-retryable close-code classification.
/// </summary>
public class SubscriptionReviewRegressionTests
{
    /// <summary>Hands out a fresh SSE stream per request so each subscriber has its own connection.</summary>
    private sealed class PerRequestSseHandler : HttpMessageHandler
    {
        public readonly List<ScriptableSseStream> Streams = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var stream = new ScriptableSseStream();
            lock (Streams)
            {
                Streams.Add(stream);
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
            return Task.FromResult(response);
        }
    }

    private static string SseNext(string customerId) =>
        "event: next\ndata: {\"data\":{\"customerAdded\":{\"customerId\":\"" + customerId +
        "\",\"customerName\":\"n\"}}}\n\n";

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(25);
        }

        Assert.Fail("Condition was not met before the timeout.");
    }

    [Fact]
    public async Task FieldErrorEvent_DoesNotTerminate_SubscriptionKeepsDelivering()
    {
        var handler = new PerRequestSseHandler();
        var options = new GraphClientOptions { SubscriptionProtocol = SubscriptionProtocol.ServerSentEvents };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://test.local/graphql/") };
        var client = new SampleClient(http, Options.Create(options), new ServiceCollection().BuildServiceProvider());
        var execute = (GraphSubscriptionExecute<Customer, Customer>)client.Subscription.CustomerAdded().Select();
        var observable = await execute.StartWithErrorsAsync();

        var results = new List<GraphResult<Customer>>();
        var terminal = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = observable.Subscribe(r =>
        {
            lock (results)
            {
                results.Add(r);
            }
        }, e => terminal.TrySetResult(e));

        await WaitUntilAsync(() => handler.Streams.Count == 1, TimeSpan.FromSeconds(5));
        await execute.WhenConnected.WaitAsync(TimeSpan.FromSeconds(5));

        // Spec-valid ExecutionResult: partial data plus a field error.
        handler.Streams[0].Push(
            "event: next\ndata: {\"data\":{\"customerAdded\":{\"customerId\":\"11111111-1111-1111-1111-111111111111\",\"customerName\":\"n\"}},\"errors\":[{\"message\":\"field failed\",\"path\":[\"customerAdded\",\"orders\"]}]}\n\n");
        handler.Streams[0].Push(SseNext("22222222-2222-2222-2222-222222222222"));

        await WaitUntilAsync(() => { lock (results) { return results.Count == 2; } }, TimeSpan.FromSeconds(5));

        Assert.False(terminal.Task.IsCompleted, "a field error must not terminate the subscription");
        lock (results)
        {
            Assert.Equal("11111111-1111-1111-1111-111111111111", results[0].Data.CustomerId.ToString());
            Assert.True(results[0].HasErrors);
            Assert.Equal("field failed", results[0].Errors[0].Message);
            Assert.False(results[1].HasErrors);
            Assert.Equal("22222222-2222-2222-2222-222222222222", results[1].Data.CustomerId.ToString());
        }
    }
    [Fact]
    public async Task WebSocket_ServerComplete_CompletesTheTypedObservable()
    {
        await using var server = await GraphQLWsTestServer.StartAsync();
        server.OnMessage = async (session, text) =>
        {
            if (text.Contains("\"subscribe\""))
            {
                using var document = JsonDocument.Parse(text);
                var id = document.RootElement.GetProperty("id").GetString()!;
                await session.SendNext(id, """{"customerAdded":{"customerId":"44444444-4444-4444-4444-444444444444","customerName":"n"}}""");
                await session.SendComplete(id);
            }
        };

        var options = new GraphClientOptions { SubscriptionProtocol = SubscriptionProtocol.GraphQLWebSocket };
        using var http = new HttpClient { BaseAddress = new Uri(server.HttpBaseAddress) };
        var client = new SampleClient(http, Options.Create(options), new ServiceCollection().BuildServiceProvider());
        var execute = (GraphSubscriptionExecute<Customer, Customer>)client.Subscription.CustomerAdded().Select();
        var observable = await execute.StartAsync();

        var events = new List<Customer>();
        var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = observable.Subscribe(
            c => events.Add(c),
            e => completed.TrySetException(e),
            () => completed.TrySetResult(true));

        await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Single(events);
    }

    [Fact]
    public async Task Overflow_InOneSubscriber_DoesNotStarveAnother()
    {
        var handler = new PerRequestSseHandler();
        var options = new GraphClientOptions
        {
            SubscriptionProtocol = SubscriptionProtocol.ServerSentEvents,
            SubscriptionMaxBufferedEvents = 1,
            SubscriptionOverflowPolicy = SubscriptionOverflowPolicy.Fault,
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://test.local/graphql/") };
        var client = new SampleClient(http, Options.Create(options), new ServiceCollection().BuildServiceProvider());
        var execute = (GraphSubscriptionExecute<Customer, Customer>)client.Subscription.CustomerAdded().Select();
        var observable = await execute.StartAsync();

        // Subscriber 1 stalls so its buffer overflows and faults.
        var firstError = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var first = observable.Subscribe(_ => Thread.Sleep(500), e => firstError.TrySetResult(e));

        var secondEvents = new List<Customer>();
        Exception? secondError = null;
        using var second = observable.Subscribe(c =>
        {
            lock (secondEvents)
            {
                secondEvents.Add(c);
            }
        }, e => secondError = e);

        await WaitUntilAsync(() => handler.Streams.Count == 2, TimeSpan.FromSeconds(5));
        await Task.Delay(200);

        // Overflow subscriber 1, then deliver one event to subscriber 2.
        var first1 = "11111111-1111-1111-1111-111111111111";
        handler.Streams[0].Push(SseNext(first1));
        await Task.Delay(100);
        handler.Streams[0].Push(SseNext("11111111-1111-1111-1111-111111111112"));
        handler.Streams[0].Push(SseNext("11111111-1111-1111-1111-111111111113"));
        await firstError.Task.WaitAsync(TimeSpan.FromSeconds(5));

        handler.Streams[1].Push(SseNext("22222222-2222-2222-2222-222222222222"));
        await WaitUntilAsync(() =>
        {
            lock (secondEvents)
            {
                return secondEvents.Count == 1;
            }
        }, TimeSpan.FromSeconds(5));

        Assert.Null(secondError);
    }

    [Theory]
    [InlineData(4400, true)]
    [InlineData(4401, true)]
    [InlineData(4004, true)]
    [InlineData(4005, true)]
    [InlineData(4406, true)]
    [InlineData(4409, true)]
    [InlineData(4429, true)]
    [InlineData(4500, true)]
    [InlineData(1002, true)]
    [InlineData(4403, false)]
    [InlineData(4408, false)]
    [InlineData(4499, false)]
    [InlineData(4504, false)]
    [InlineData(1000, false)]
    [InlineData(1006, false)]
    public void CloseCode_FatalClassification_MatchesGraphQlWsReferenceClient(int code, bool fatal)
    {
        Assert.Equal(fatal, WSClient.IsFatalCloseCodeForTests(code));
    }
}
