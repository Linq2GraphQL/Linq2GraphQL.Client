using Linq2GraphQL.Client;
using System.Reactive.Linq;
using Linq2GraphQL.Client.Subscriptions;
using Linq2GraphQL.TestClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.Tests;

/// <summary>
///     End-to-end wiring tests: the transport → SubscriptionEventBuffer → processor →
///     observer chain in GraphSubscriptionExecute, driven by a fake SSE server. The
///     buffer itself is unit-tested in SubscriptionEventBufferTests; these tests pin
///     the overflow→observer behavior of the whole pipeline.
/// </summary>
public class SubscriptionBufferWiringTests
{
    private sealed record Wiring(
        HttpClient HttpClient,
        SampleClient Client,
        ScriptableSseStream Stream,
        GraphSubscriptionExecute<Customer, Customer> Execute,
        List<Customer> Events,
        TaskCompletionSource<Exception> Error,
        TaskCompletionSource<bool> Completed) : IDisposable
    {
        public void Dispose()
        {
            HttpClient.Dispose();
        }
    }

    private static async Task<Wiring> CreateAsync(GraphClientOptions options, int observerDelayMs = 0)
    {
        var handler = FakeSseHandler.Streaming(out var stream);
        options.SubscriptionProtocol = SubscriptionProtocol.ServerSentEvents;
        options.UseSafeMode = false;
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test.local/graphql/") };
        var sampleClient = new SampleClient(httpClient, Options.Create(options), new ServiceCollection().BuildServiceProvider());
        var execute = (GraphSubscriptionExecute<Customer, Customer>)sampleClient.Subscription.CustomerAdded().Select();

        IObservable<Customer> observable = await execute.StartAsync();
        var events = new List<Customer>();
        var error = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        observable.Subscribe(
            c =>
            {
                events.Add(c);
                if (observerDelayMs > 0)
                {
                    // Runs inline on the processor thread: parks the drain so the test
                    // can fill the buffer deterministically while delivery is stalled.
                    Thread.Sleep(observerDelayMs);
                }
            },
            e => error.TrySetResult(e),
            () => completed.TrySetResult(true));

        // The transport is live only once the subscription is created; this is the
        // deterministic point from which pushed frames are guaranteed to be seen.
        await execute.WhenConnected.WaitAsync(TimeSpan.FromSeconds(10));

        return new Wiring(httpClient, sampleClient, stream, execute, events, error, completed);
    }

    private static string PayloadJson(string id)
    {
        var payload =
            "{\"data\":{\"customerAdded\":{\"customerId\":\"" + id + "\",\"customerName\":\"n\"}}}";
        return "event: next\ndata: " + payload + "\n\n";
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.Fail("Condition was not met before the timeout.");
    }

    [Fact]
    public async Task FaultPolicy_Overflow_ObserverGetsInvalidOperationException()
    {
        using var wiring = await CreateAsync(new GraphClientOptions
        {
            SubscriptionMaxBufferedEvents = 1,
            SubscriptionOverflowPolicy = SubscriptionOverflowPolicy.Fault,
        }, observerDelayMs: 300);

        wiring.Stream.Push(PayloadJson("10000000-0000-0000-0000-000000000001"));
        await Task.Delay(50); // the processor has picked up event 1 and is now stalled
        wiring.Stream.Push(
            PayloadJson("10000000-0000-0000-0000-000000000002") +
            PayloadJson("10000000-0000-0000-0000-000000000003"));

        var exception = await wiring.Error.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("buffer", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(wiring.Completed.Task.IsCompleted); // faulted, not completed
        // The processor drains already-buffered events before surfacing the fault:
        // event 1 delivered, event 2 was buffered pre-overflow, event 3 triggered it.
        Assert.Equal(2, wiring.Events.Count);
        Assert.Equal(new Guid("10000000-0000-0000-0000-000000000002"),
            wiring.Events[^1].CustomerId);
    }

    [Fact]
    public async Task KeepLatestPolicy_Overflow_DeliversOnlyLatest()
    {
        using var wiring = await CreateAsync(new GraphClientOptions
        {
            SubscriptionMaxBufferedEvents = 1,
            SubscriptionOverflowPolicy = SubscriptionOverflowPolicy.KeepLatest,
        }, observerDelayMs: 300);

        wiring.Stream.Push(PayloadJson("20000000-0000-0000-0000-000000000001"));
        await Task.Delay(50);
        wiring.Stream.Push(
            PayloadJson("20000000-0000-0000-0000-000000000002") +
            PayloadJson("20000000-0000-0000-0000-000000000003"));

        // Deterministic: event 1 delivered; overflow dropped event 2, kept event 3.
        await WaitUntilAsync(() => wiring.Events.Count >= 2, TimeSpan.FromSeconds(10));

        Assert.Equal(2, wiring.Events.Count);
        Assert.Equal(new Guid("20000000-0000-0000-0000-000000000003"),
            wiring.Events[^1].CustomerId); // the newest event always survives
        Assert.False(wiring.Error.Task.IsFaulted);
    }

    [Fact]
    public async Task DropOldestPolicy_Overflow_DeliversNewestWindow()
    {
        using var wiring = await CreateAsync(new GraphClientOptions
        {
            SubscriptionMaxBufferedEvents = 2,
            SubscriptionOverflowPolicy = SubscriptionOverflowPolicy.DropOldest,
        }, observerDelayMs: 300);

        wiring.Stream.Push(PayloadJson("30000000-0000-0000-0000-000000000001"));
        await Task.Delay(50);
        wiring.Stream.Push(
            PayloadJson("30000000-0000-0000-0000-000000000002") +
            PayloadJson("30000000-0000-0000-0000-000000000003") +
            PayloadJson("30000000-0000-0000-0000-000000000004"));

        // Deterministic: event 1 delivered; events 2+3 dropped oldest-first, 4 survives.
        await WaitUntilAsync(() => wiring.Events.Count >= 3, TimeSpan.FromSeconds(10));

        Assert.Equal(3, wiring.Events.Count);
        Assert.Equal(new Guid("30000000-0000-0000-0000-000000000004"),
            wiring.Events[^1].CustomerId);
        Assert.True(wiring.Events[^1].CustomerId > wiring.Events[0].CustomerId); // order preserved
        Assert.False(wiring.Error.Task.IsFaulted);
    }

    [Fact]
    public async Task NoOverflow_AllEventsDelivered_InOrder()
    {
        using var wiring = await CreateAsync(new GraphClientOptions());

        wiring.Stream.Push(
            PayloadJson("40000000-0000-0000-0000-000000000001") +
            PayloadJson("40000000-0000-0000-0000-000000000002") +
            PayloadJson("40000000-0000-0000-0000-000000000003"));

        await WaitUntilAsync(() => wiring.Events.Count == 3, TimeSpan.FromSeconds(10));
        await Task.Delay(100);

        Assert.Equal(3, wiring.Events.Count);
        Assert.Equal(new Guid("40000000-0000-0000-0000-000000000001"), wiring.Events[0].CustomerId);
        Assert.Equal(new Guid("40000000-0000-0000-0000-000000000002"), wiring.Events[1].CustomerId);
        Assert.Equal(new Guid("40000000-0000-0000-0000-000000000003"), wiring.Events[2].CustomerId);
        Assert.False(wiring.Error.Task.IsFaulted);
    }
}
