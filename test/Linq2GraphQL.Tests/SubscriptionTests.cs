using Linq2GraphQL.TestClient;

namespace Linq2GraphQL.Tests;

public class SubscriptionTests : IClassFixture<SampleClientFixture>
{
    private readonly SampleClient sampleClient;

    public SubscriptionTests(SampleClientFixture safeModeClient)
    {
        sampleClient = safeModeClient.sampleClient;
    }

    [Fact]
    public async Task NoParameter()
    {
        var customerName = "JockeD";
        var customerInput = new CustomerInput
        {
            CustomerName = customerName, CustomerId = Guid.NewGuid(), Status = CustomerStatus.Active, Orders = new()
        };

        var subscriptionExecute = sampleClient
            .Subscription
            .CustomerAdded()
            .Select();

        var subscription = await subscriptionExecute.StartAsync();

        // The subscription must actually deliver the event pushed by the mutation.
        var received = new TaskCompletionSource<Customer>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        long receivedCount = 0;
        subscription.Subscribe(
            c =>
            {
                Interlocked.Increment(ref receivedCount);
                received.TrySetResult(c);
            },
            e => failed.TrySetResult(e));

        // Wait for the transport to be connected instead of guessing with a delay —
        // otherwise the mutation can fire before the server has registered us.
        await subscriptionExecute.WhenConnected;

        var newCustomer = await sampleClient
            .Mutation
            .AddCustomer(customerInput).Select().ExecuteAsync();

        Assert.Equal(customerName, newCustomer?.CustomerName);

        var completed = await Task.WhenAny(received.Task, failed.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        if (completed == failed.Task)
        {
            // Await rethrows the original subscription failure with full detail.
            await failed.Task;
        }

        if (completed != received.Task)
        {
            Assert.Fail(
                $"Subscription did not deliver the mutated customer within 10 seconds (mutation returned '{newCustomer?.CustomerName}').");
        }

        var delivered = await received.Task;
        Assert.Equal(customerName, delivered.CustomerName);
        Assert.True(Interlocked.Read(ref receivedCount) >= 1);
    }
}