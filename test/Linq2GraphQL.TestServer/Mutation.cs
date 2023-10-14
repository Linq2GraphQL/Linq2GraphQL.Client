using HotChocolate.Subscriptions;
using Linq2GraphQL.TestServer.Models;

namespace Linq2GraphQL.TestServer;

public class Mutation
{
    public string SetName(string name)
    {
        return "New Name is: " + name;
    }

    public async Task<Customer> AddCustomer(Customer customer, [Service] ITopicEventSender sender)
    {
        //This is where we should do the actual work


        customer.Orders = new List<Order> { new(customer) { OrderDate = DateTimeOffset.Now } };

        //Send a subscription
        await sender.SendAsync(nameof(Subscription.CustomerAdded), customer);


        //Send a subscription with a dynamic topic
        await sender.SendAsync(customer.CustomerName, customer);

        return customer;
    }
}