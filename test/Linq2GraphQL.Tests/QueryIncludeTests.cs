using GreenDonut;
using Linq2GraphQL.TestClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linq2GraphQL.Tests
{

    public class QueryIncludeTests : IClassFixture<SampleClientFixture>
    {
        private readonly SampleClient sampleClient;

        public QueryIncludeTests(SampleClientFixture safeModeClient)
        {
            sampleClient = safeModeClient.sampleClient;
        }

        [Fact]
        public async Task IncludePrimitives()
        {
            var result = await sampleClient
                .Query
                .Orders()
                .Select(e => e.Nodes)
                .ExecuteAsync();

            Assert.NotEqual(Guid.Empty, result.First().OrderId);

            Assert.Null(result.First().Address);
        }


        [Fact]
        public async Task IncludePrimitives_MultipleLevels()
        {
            var query = sampleClient
                .Query
                .Orders(first: 1)
                .Include(e => e.Nodes.Select(e => e.Customer)) //TODO Should this be loaded automatically because it is chained in orders?
                .Include(e => e.Nodes.Select(e => e.Customer.Orders))
                .Select(e => e.Nodes);


            var result = await query.ExecuteAsync();

            var order = result.First();
            Assert.NotEqual(Guid.Empty, order.OrderId);

            Assert.NotNull(order.Customer);
            Assert.NotNull(order.Customer.CustomerName);
            Assert.NotNull(order.Customer.Orders);
        }

        [Fact]
        public async Task IncludePrimitives_AnomousResult()
        {
            var query = sampleClient
                .Query
                .Orders()
                .Select(e => e.Nodes.Select(n => new
                {
                    n.OrderId,
                    Hello = n.OrderHello("Peter", 1234),
                    DeliveryAddress = n.OrderAddress(AddressType.Delivery),
                    InvoiceAddress = n.OrderAddress(AddressType.Invoice),
                    Cust = new
                    {
                        n.Customer.CustomerName,
                        n.Customer.Orders
                    }
                }));


            var order = (await query.ExecuteAsync()).First();

            Assert.NotNull(order.Cust?.CustomerName);
            Assert.NotNull(order.Cust?.Orders);
            Assert.NotNull(order.Hello);
            Assert.NotEqual(Guid.Empty, order.Cust?.Orders.First().OrderId);
            Assert.NotEqual(Guid.Empty, order.OrderId);

            //Check no over fetch
            var baseResult = query.BaseResult;
            Assert.Equal(0, baseResult.TotalCount);
            Assert.Equal(DateTimeOffset.MinValue, baseResult.Nodes.First().OrderDate);


        }



    }

}
