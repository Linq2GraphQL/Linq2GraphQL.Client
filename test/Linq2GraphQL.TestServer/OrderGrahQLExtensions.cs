using Linq2GraphQL.TestServer.Data;
using Linq2GraphQL.TestServer.Models;

namespace Linq2GraphQL.TestServer
{
    [ExtendObjectType(typeof(Customer))]
    public class OrderGraphQLExtensions
    {
        public List<Customer> RelatedCustomers(int relationType)
        {
            return SampleData.GetCustomers();
        }

        public Customer RelatedCustomer(int relationType)
        {
            return SampleData.GetCustomers().FirstOrDefault();
        }

    }
}
