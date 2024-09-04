using System.Collections.Generic;
using System;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClientNullable;

public class MutationMethods
{
    private readonly GraphClient client;

    public MutationMethods(GraphClient client)
    {
        this.client = client;
    }

    public GraphQuery<Customer?> UpdateCustomer(CustomerInput customer)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("customer","CustomerInput!", customer),
        };

        return new GraphQuery<Customer?>(client,  "updateCustomer", OperationType.Mutation, arguments); 
    }

    }