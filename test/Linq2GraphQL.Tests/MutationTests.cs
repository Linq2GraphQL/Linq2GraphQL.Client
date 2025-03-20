using Linq2GraphQL.TestClient;

namespace Linq2GraphQL.Tests;

public class MutationTests : IClassFixture<SampleClientFixture>
{
    private readonly SampleClient sampleClient;

    public MutationTests(SampleClientFixture safeModeClient)
    {
        sampleClient = safeModeClient.sampleClient;
    }

    [Theory]
    [InlineData("Magnus")]
    [InlineData("Jocke")]
    public async Task SetName_WithName_PrependNameNewName(string name)
    {
        var result = await sampleClient
            .Mutation
            .SetName(name)
            .Select()
            .ExecuteAsync();

        Assert.Equal($"New Name is: {name}", result);
    }

    [Fact]
    public async Task Mutation_Multiple()
    {
        var id = Guid.NewGuid();
        var customerId = await sampleClient
            .Mutation
            .AddCustomer(new()
            {
                CustomerId = id, CustomerName = "New Customer", Orders = new(), Status = CustomerStatus.Active
            })
            .Select(e => e.CustomerId)
            .ExecuteAsync();

        Assert.Equal(id, customerId);
    }
}