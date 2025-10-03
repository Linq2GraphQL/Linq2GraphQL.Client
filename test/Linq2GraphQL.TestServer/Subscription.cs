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
    [Topic("{name}")]
    public Customer CustomerNameAdded(string name, [EventMessage] Customer customer)
    {
        return customer;
    }
}