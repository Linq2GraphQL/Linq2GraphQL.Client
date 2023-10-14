using GreenDonut;
using Linq2GraphQL.TestClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linq2GraphQL.Tests
{

    public class QueryArgumentTests : IClassFixture<SampleClientFixture>
    {
        private readonly SampleClient sampleClient;

        public QueryArgumentTests(SampleClientFixture safeModeClient)
        {
            sampleClient = safeModeClient.sampleClient;
        }

        [Fact]
        public async Task TopLevelArguments()
        {
            var query = sampleClient
                .Query
                .Orders(first: 1)
                .Include(e => e.Nodes.Select(e => e.OrderHello("JOcke")))
                .Include(e => e.Nodes.Select(e => e.OrderAddress(AddressType.Invoice)))
                .Select(e => e.Nodes);

            await query.InitQueryAsync();
            var tt = query.GetGraphQLQuery();

            var result = await query.ExecuteAsync();

            Assert.Single(result);
        }

        [Fact]
        public async Task InQueryArguments()
        {
            var query = sampleClient
                .Query
                .Orders(first: 1)
                .Include(e => e.Nodes.Select(e => e.OrderAddress(AddressType.Invoice)))
                .Select(e => e.Nodes);

            var bb = query.GetGraphQLQuery();

            var result = await query.ExecuteAsync();

            Assert.NotNull(result.First().OrderAddress);
            Assert.Equal("Invoice", result.First().OrderAddress.Name);
        }

        [Fact]
        public async Task InQueryAnonymousType()
        {
            var query = sampleClient
                .Query
                .Orders()
                .Select(e => e.Nodes.Select(e => e.OrderHello("sdd")));

            var gQ = query.GetGraphQLQuery();
            var result = await query.ExecuteAsync();

            Assert.NotNull(result.First());
        
        }


    }

}
