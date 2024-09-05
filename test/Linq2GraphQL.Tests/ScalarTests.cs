using Linq2GraphQL.TestClient;
using Linq2GraphQL.TestClientNullable;


namespace Linq2GraphQL.Tests
{



    public class ScalarTests : IClassFixture<SampleNullableClientFixture>
    {
        private readonly SampleNullableClient sampleClient;

        public ScalarTests(SampleNullableClientFixture safeModeClient)
        {
            sampleClient = safeModeClient.sampleClient;
        }

        [Fact]
        public async Task GetScalar()
        {
            var result = await sampleClient
          .Query
          .Person()
          .Select()
          .ExecuteAsync();

            var macAddress = result;

            Assert.NotNull(macAddress);
        }

        [Fact]
        public async Task SetScalar()
        {
            var result = await sampleClient
          .Mutation
          .UpdatePerson(person: new PersonInput { Name = "Peter", MacAddress= new MacAddress("01-23-45-67-89-ab")  })
          .Select()
          .ExecuteAsync();

            var macAddress = result.MacAddress;

            Assert.NotNull(macAddress);
        }


    }

}

