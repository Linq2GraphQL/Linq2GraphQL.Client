using Linq2GraphQL.TestClient;

namespace Linq2GraphQL.Tests
{

    public class QueryProjectionTests : IClassFixture<SampleClientFixture>
    {
        private readonly SampleClient sampleClient;

        public QueryProjectionTests(SampleClientFixture safeModeClient)
        {
            sampleClient = safeModeClient.sampleClient;
        }

        [Fact]
        public async Task ProjectingAnomousObject()
        {
            var query = sampleClient
                .Query
                .Orders()
                .Select(e => e.Nodes.Select(o => new OrderIdAddress { OrderId = o.OrderId, Address = o.Address }));

            var request = await query.GetRequestAsync();
            var result = await query.ExecuteAsync();
            Assert.NotEqual(Guid.Empty, result.First().OrderId);
            Assert.NotNull(result.First().Address);
           
            var baseOrder = query.BaseResult.Nodes.First();
            Assert.Null(baseOrder.Customer);
            Assert.Equal(DateTimeOffset.MinValue, baseOrder.OrderDate);

        }

        [Fact]
        public async Task ProjectingToType()
        {
            var query = sampleClient
                .Query
                .Orders()
                .Select(e => e.Nodes.Select(o => new OrderIdAddress { OrderId = o.OrderId, Address = o.Address }))
               ;

      
            var result = await query.ExecuteAsync();
            var order = result.First();
            Assert.NotEqual(Guid.Empty, order.OrderId);
            Assert.NotNull(order.Address);

            var baseOrder = query.BaseResult.Nodes.First();
            Assert.Null(baseOrder.Customer);
            Assert.Equal(DateTimeOffset.MinValue, baseOrder.OrderDate);

        }




    }

    public class  OrderIdAddress 
    {
        public Guid OrderId { get; set; }
        public Address? Address { get; set; }
    }

}
