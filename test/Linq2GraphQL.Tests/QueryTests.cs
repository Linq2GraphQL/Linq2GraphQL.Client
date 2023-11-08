using FluentAssertions;
using Linq2GraphQL.TestClient;

namespace Linq2GraphQL.Tests;

public class QueryTests : IClassFixture<SampleClientFixture>
{
    private readonly SampleClient sampleClient;

    public QueryTests(SampleClientFixture safeModeClient)
    {
        sampleClient = safeModeClient.sampleClient;
    }

    [Theory]
    [InlineData("Magnus")]
    [InlineData("Jocke")]
    public async Task Hello_WithName_PrependNameHello(string name)
    {
        var result = await sampleClient
            .Query
            .Hello(name)
            .Select()
            .ExecuteAsync();

        Assert.Equal($"Hello, {name}!", result);
    }

    [Fact]
    public async Task Hello_WithNoName_HelloWorld()
    {
        var result = await sampleClient
            .Query
            .Hello()
            .Select()
            .ExecuteAsync();

        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public async Task Customers_WithNoInclude()
    {
        var result = await sampleClient
            .Query
            .Customers()
            .Select()
            .ExecuteAsync();

        Assert.Equal(2, result.Count);
        var customer1 = result[0];
        Assert.NotEqual(default, customer1.CustomerId);
        Assert.Null(customer1.Orders);
    }

    [Fact]
    public async Task Customers_WithIncludeSelect()
    {
        var query = sampleClient
            .Query
            .Customers()
            .Include(e => e.Select(e=> new { e.CustomerId, e.Status }))
            .Select();

        var result = await query.ExecuteAsync();

        var schema = query.Schema;

        var customer1 = result[0];
        Assert.NotEqual(default, customer1.CustomerId);
        Assert.Equal(default, customer1.CustomerName);

    }


    [Fact]
    public async Task Customers_WithIncludeOrders()
    {
        var query = sampleClient
            .Query
            .Customers()
            .Include(e => e.Select(e => e.Orders))
            .Select();
          
        var result = await query.ExecuteAsync();

        var customer1 = result[0];
        Assert.NotNull(customer1.Orders);
        Assert.NotEqual(default, customer1.CustomerId);
    }

    [Fact]
    public async Task Customers_IncludeNodes()
    {
        var orders = await sampleClient
            .Query
            .Orders()
            .Include(x => x.Nodes)
            .Select().ExecuteAsync();

        var order = orders.Nodes[0];
        Assert.NotNull(order);
    }

    [Fact]
    public async Task Customers_WithIncludeOrdersAndAddress()
    {
        var result = await sampleClient
            .Query
            .Customers()
            .Include(e => e.Select(e => e.Orders.Select(f => f.Address)))
            .Include(e => e.Select(e => e.Orders.Select(f => f.Customer)))
           
            .Select()
            .ExecuteAsync();

        var customer1 = result[0];
        Assert.NotNull(customer1.Orders.First().Address?.Street);
        Assert.Equal(default, customer1.Orders.First().OrderId);
    }

    [Fact]
    public async Task Customers_Include_Nested()
    {
        var query = sampleClient
            .Query
            .Customers()
            .Include(e => e.Select(e => e.Orders.Select(f => f.Address)))
            .Include(e => e.Select(e => e.Orders))
            .Include(e => e.Select(e => e.Orders.Select(f => f.Customer.CustomerName)))
            .Select();

        var result = await query.ExecuteAsync();
        var baeResult = query.BaseResult;

        var order = result[0].Orders.First();
        Assert.NotNull(order.Address?.Street);
        Assert.NotNull(order.Customer?.CustomerName);
        Assert.Equal(default, order.Customer.CustomerId);
    }


    [Fact]
    public async Task Customers_WithSingleSelect()
    {
        var result = await sampleClient
            .Query
            .Customers()
            .Select(c => c.Select(e => e.CustomerName))
            .ExecuteAsync();

        var customerName = result.First();
        Assert.NotNull(customerName);
    }

    [Fact]
    public async Task Customers_WithAnonymousSelect()
    {
        var result = await sampleClient
            .Query
            .Customers()
            .Select(c => c.Select(e => new { e.CustomerId, e.CustomerName }))
            .ExecuteAsync();

        var customer = result.First();
        Assert.NotNull(customer);
    }


    [Fact]
    public async Task Orders_WithSelect()
    {
        var result = await sampleClient
            .Query
            .Orders()
            .Select()
            .ExecuteAsync();


        Assert.NotNull(result);
        Assert.Null(result.Nodes);
    }



    [Fact]
    public async Task Orders_WithSelectIncludeNodes()
    {
        var result = await sampleClient
            .Query
            .Orders()
            .Include(e => e.Nodes)
            .Select()
            .ExecuteAsync();

        Assert.NotNull(result?.Nodes);
    }

    [Fact]
    public async Task Orders_WithSelectNodesIncludeAddress()
    {
        var result = await sampleClient
            .Query
            .Orders()
            .Include(e => e.Nodes.Select(e => e.Address))
            .Select(e => e.Nodes)
            .ExecuteAsync();

        Assert.True(result.All(e => e.Address != null));
    }

    [Fact]
    public async Task Orders_ChainedSelect_2()
    {
        var query = sampleClient
            .Query
            .Orders()
            .Select(e => e.Nodes.Select(e => e.Customer.Orders.Select(e => new { e.Address, e.OrderId })));

        var result = await query.ExecuteAsync();
        var baseType = query.BaseResult;

        Assert.Equal(default, baseType.Nodes.First().OrderDate);

    }


    [Fact]
    public async Task Orders_ChainedSelect()
    {
        var query = sampleClient
            .Query
            .Orders()
            .Include(e => e.Nodes.Select(e => e.Customer.Orders.Select(e => e.Address)))
            .Select(e => e.Nodes);

        var result = await query.ExecuteAsync();
        var orderAddresses = result.SelectMany(e => e.Customer.Orders.Select(a => a.Address));

        Assert.True(orderAddresses.All(e => e != null));

    }

    [Fact]
    public async Task Orders_WithOrderByDateDesc()
    {
        var result = await sampleClient
            .Query
            .Orders(order: new List<OrderSortInput> { new() { OrderDate = SortEnumType.Desc } })
            .Select(e => e.Nodes)
            .ExecuteAsync();

        result.Should().BeInDescendingOrder(e => e.OrderDate);
    }

  


    [Fact]
    public async Task QueryReturnNull_Object()
    {
        var result = await sampleClient
            .Query
            .CustomerReturnNull()
            .Select()
            .ExecuteAsync();

        Assert.True(result == null);

    }

    [Fact]
    public async Task QueryReturnNull_AnonymousObject()
    {
        var result = await sampleClient
            .Query
            .CustomerReturnNull()
            .Select(e => new { e.CustomerId, e.CustomerName })
            .ExecuteAsync();

        Assert.True(result == null);

    }

    [Fact]
    public async Task QueryReturnNull_Default()
    {
        var result = await sampleClient
            .Query
            .CustomerReturnNull()
            .Select(e => e.CustomerId)
            .ExecuteAsync();

        Assert.True(result == Guid.Empty);

    }

    [Fact]
    public async Task QueryReturnNull_GuidNull()
    {
        var result = await sampleClient
            .Query
            .CustomerReturnNull()
            .Select<Guid?>(e => e.CustomerId)
            .ExecuteAsync();

        Assert.True(result == null);

    }

}