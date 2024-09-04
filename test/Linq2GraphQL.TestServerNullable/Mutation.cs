using Linq2GraphQL.TestServer.Models;

namespace Linq2GraphQL.TestServerNullable
{
    public class Mutation
    {

        public Customer? UpdateCustomer(Guid customerId, string name)
        {
            return new Customer { CustomerId = customerId, CustomerName = name };
        }

    }
}
