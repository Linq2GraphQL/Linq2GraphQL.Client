using Linq2GraphQL.TestServer.Data;
using Linq2GraphQL.TestServer.Models;


namespace Linq2GraphQL.TestServerNullable
{
    public class Query
    {
        public List<Customer> GetCustomerList()
        {
            var customers = SampleData.GetCustomers();
            return customers;
        }

        public Customer? GetCustomerNullable()
        {
            return null;
        }

        public List<Customer?>? GetCustomerListAllNullable()
        {
            return null;
        }

        public List<Customer>? GetCustomerListNullable()
        {
            return null;
        }
    }
}
