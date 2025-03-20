using System.Text;
using Linq2GraphQL.TestClientNullable;

namespace Linq2GraphQL.Tests;

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
            .Include(e => e.Data)
            .Select()
            .ExecuteAsync();

        var data = Encoding.UTF8.GetString(result.Data!.ToArray());

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

        var customer = result.FirstOrDefault()?.FirstOrDefault();
        Assert.NotNull(customer);
    }
}