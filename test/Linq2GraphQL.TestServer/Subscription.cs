using Linq2GraphQL.TestServer.Models;

namespace Linq2GraphQL.TestServer;

[SubscriptionType]
public class Subscription
{
    [Subscribe]
    public Customer CustomerAdded([EventMessage] Customer customer)
    {
        return customer;
    }

    [Subscribe]
    [Topic($"{{{nameof(name)}}}")]
    public Customer CustomerNameAdded(string name, [EventMessage] Customer customer)
    {
        Console.WriteLine($"Customer {customer.CustomerName} added from topic {name}");
        return customer;
    }
}