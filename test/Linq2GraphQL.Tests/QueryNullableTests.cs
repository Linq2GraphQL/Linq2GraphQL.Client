using Linq2GraphQL.TestClient;
using Linq2GraphQL.TestClientNullable;
using System.Collections;
using System.Text;


namespace Linq2GraphQL.Tests
{
    public class QueryNullableTests : IClassFixture<SampleNullableClientFixture>
    {
        private readonly SampleNullableClient nullableClient;

        public QueryNullableTests(SampleNullableClientFixture nullableFixture)
        {
            nullableClient = nullableFixture.sampleClient;
        }

        [Fact]
        public async Task GetCustomers()
        {
            var result = await nullableClient
                .Query
                .CustomerList()
                .Select()
                .ExecuteAsync();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetItemData()
        {
            var result = await nullableClient
                .Query
                .Item()
                .Include(e=> e.Data)
                .Select()
                .ExecuteAsync();

            var data = System.Text.Encoding.UTF8.GetString(result.Data!.ToArray());

            Assert.Equal(result.ItemName, data);
        }


        [Fact]
        public async Task GetCustomerNull()
        {
            var result = await nullableClient
                .Query
                .CustomerNullable()
                .Select()
                .ExecuteAsync();

            Assert.Null(result);
        }


        [Fact]
        public async Task GetCustomerListInList()
        {
            var result = await nullableClient
                .Query
                .CustomerListInList()
                .Select()
                .ExecuteAsync();

            TestClientNullable.Customer? customer = result.FirstOrDefault()?.FirstOrDefault();
            Assert.NotNull(customer);
        }

    }
}
