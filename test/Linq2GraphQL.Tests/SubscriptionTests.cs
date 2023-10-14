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
    public async void NoParameter()
    {
        var customerName = "JockeD";
        var customerInput = new CustomerInput
        {
            CustomerName = customerName,
            CustomerId = Guid.NewGuid(),
            Status = CustomerStatus.Active,
            Orders = new()
        };

        var subscription = await sampleClient
            .Subscription
            .CustomerAdded()
            .Select()
            .StartAsync();

        Customer? customer = null;
        subscription.Subscribe(c => { customer = c; });

        var newCustomer = await sampleClient
            .Mutation
            .AddCustomer(customerInput).Select().ExecuteAsync();

        await Task.Delay(500);
        Assert.Equal(customerName, newCustomer?.CustomerName);
    }
}