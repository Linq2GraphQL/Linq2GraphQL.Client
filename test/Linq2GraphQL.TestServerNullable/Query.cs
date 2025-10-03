using Linq2GraphQL.TestServer.Data;
using Linq2GraphQL.TestServer.Models;


namespace Linq2GraphQL.TestServerNullable
{
    [QueryType]
    public class Query
    {

        public Item GetItem()
        {
            return SampleData.GetItem("Test Item");
        }

        [Obsolete("This is an really old method! please d not use it!!")]
        public Item GetItemDraft()
        {
            return SampleData.GetItem("Test Item");
        }

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

        public List<List<Customer>> GetCustomerListInList()
        {
            return [SampleData.GetCustomers()];
        }

        public Person GetPerson()
        {
            return new Person { Name = "Jocke D", MacAddress = "00-1B-63-84-45-E6", Longitude = 14.4 };

        }

       


    }

    public class Person
    {
        public required string Name { get; set; }

        [GraphQLType(typeof(MacAddressType))]
        public required string MacAddress { get; set; }

        [GraphQLType(typeof(LongitudeType))]
        public double Longitude { get; set; }
    }

}
