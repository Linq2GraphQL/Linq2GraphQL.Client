using Linq2GraphQL.TestServer.Models;

namespace Linq2GraphQL.TestServerNullable
{
    [MutationType]
    public class Mutation
    {

        public Customer UpdateCustomer(Customer customer)
        {
            return customer;
        }

        public Person UpdatePerson(Person person)
        {
            return person;

        }

    }
}
