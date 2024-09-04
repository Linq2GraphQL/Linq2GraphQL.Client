using System.Collections.Generic;
using System;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

public class QueryMethods
{
    private readonly GraphClient client;

    public QueryMethods(GraphClient client)
    {
        this.client = client;
    }

    public GraphQuery<string> Hello(string name = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("name","String", name),
        };

        return new GraphQuery<string>(client,  "hello", OperationType.Query, arguments); 
    }

    public GraphQuery<Customer> CustomerReturnNull()
    {
	    var arguments = new List<ArgumentValue>
        {
        };

        return new GraphQuery<Customer>(client,  "customerReturnNull", OperationType.Query, arguments); 
    }

    public GraphQuery<List<Customer>> Customers()
    {
	    var arguments = new List<ArgumentValue>
        {
        };

        return new GraphQuery<List<Customer>>(client,  "customers", OperationType.Query, arguments); 
    }

    public GraphQuery<OrdersNoBackwardPaginationConnection> OrdersNoBackwardPagination(int? first = null, string after = null, OrderFilterInput where = null, List<OrderSortInput> order = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("first","Int", first),
    	    new("after","String", after),
    	    new("where","OrderFilterInput", where),
    	    new("order","[OrderSortInput!]", order),
        };

        return new GraphQuery<OrdersNoBackwardPaginationConnection>(client,  "ordersNoBackwardPagination", OperationType.Query, arguments); 
    }

    public GraphCursorQuery<OrdersNoTotalCountConnection> OrdersNoTotalCount(int? first = null, string after = null, int? last = null, string before = null, OrderFilterInput where = null, List<OrderSortInput> order = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("first","Int", first),
    	    new("after","String", after),
    	    new("last","Int", last),
    	    new("before","String", before),
    	    new("where","OrderFilterInput", where),
    	    new("order","[OrderSortInput!]", order),
        };

        return new GraphCursorQuery<OrdersNoTotalCountConnection>(client,  "ordersNoTotalCount", OperationType.Query, arguments); 
    }

    public GraphCursorQuery<OrdersConnection> Orders(int? first = null, string after = null, int? last = null, string before = null, OrderFilterInput where = null, List<OrderSortInput> order = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("first","Int", first),
    	    new("after","String", after),
    	    new("last","Int", last),
    	    new("before","String", before),
    	    new("where","OrderFilterInput", where),
    	    new("order","[OrderSortInput!]", order),
        };

        return new GraphCursorQuery<OrdersConnection>(client,  "orders", OperationType.Query, arguments); 
    }

    public GraphCursorQuery<AnimalsConnection> Animals(int? first = null, string after = null, int? last = null, string before = null, IAnimalFilterInput where = null, List<IAnimalSortInput> order = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("first","Int", first),
    	    new("after","String", after),
    	    new("last","Int", last),
    	    new("before","String", before),
    	    new("where","IAnimalFilterInput", where),
    	    new("order","[IAnimalSortInput!]", order),
        };

        return new GraphCursorQuery<AnimalsConnection>(client,  "animals", OperationType.Query, arguments); 
    }

    public GraphQuery<OrdersOffsetPagingCollectionSegment> OrdersOffsetPaging(int? skip = null, int? take = null, OrderFilterInput where = null, List<OrderSortInput> order = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("skip","Int", skip),
    	    new("take","Int", take),
    	    new("where","OrderFilterInput", where),
    	    new("order","[OrderSortInput!]", order),
        };

        return new GraphQuery<OrdersOffsetPagingCollectionSegment>(client,  "ordersOffsetPaging", OperationType.Query, arguments); 
    }

    }
