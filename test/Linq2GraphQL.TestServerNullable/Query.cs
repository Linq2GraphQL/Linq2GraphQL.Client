﻿using Linq2GraphQL.TestServer.Data;
using Linq2GraphQL.TestServer.Models;


namespace Linq2GraphQL.TestServerNullable
{
    public class Query
    {
        public List<Customer> GetCustomerList()
        {
            return SampleData.GetCustomers();
        }

        public Customer? GetCustomerNullable()
        {
            return null;
        }

    }
}
