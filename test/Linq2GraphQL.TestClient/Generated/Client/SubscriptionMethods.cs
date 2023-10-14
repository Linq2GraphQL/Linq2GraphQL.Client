using System.Collections.Generic;
using System;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Subscriptions;

namespace Linq2GraphQL.TestClient;

public class SubscriptionMethods
{
    private readonly GraphClient client;

    public SubscriptionMethods(GraphClient client)
    {
        this.client = client;
    }

    public GraphSubscription<Customer> CustomerAdded()
    {
	    var arguments = new List<ArgumentValue>
        {
        };

        return new GraphSubscription<Customer>(client,  "customerAdded", OperationType.Subscription, arguments); 
    }

    public GraphSubscription<Customer> CustomerNameAdded(string name = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("name","String", name),
        };

        return new GraphSubscription<Customer>(client,  "customerNameAdded", OperationType.Subscription, arguments); 
    }

    }
