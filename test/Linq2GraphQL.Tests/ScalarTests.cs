﻿using Linq2GraphQL.TestClient;
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

            Assert.NotNull(result.MacAddress);
            Assert.NotNull(result.Longitude);
        }

        [Fact]
        public async Task SetScalar()
        {
            var result = await sampleClient
          .Mutation
          .UpdatePerson(person: new PersonInput { Name = "Peter", MacAddress = new MacAddress("01-23-45-67-89-ab"), Longitude = new Longitude {  Value = "14° 24' 0\" E" } })
          .Select()
          .ExecuteAsync();



            Assert.NotNull(result);
        }


    }

}
