using Linq2GraphQL.TestClient;

namespace Linq2GraphQL.Tests;

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
    public async Task IncludeMethod_MultipleLevels()
    {
        var query = sampleClient
            .Query
            .Customers()
       .Include(e => e.Select(c => c.RelatedCustomer(2).Orders.Select(o => o.Address)))
       .Select();

        var req = await query.GetRequestAsJsonAsync();
        var result = await query.ExecuteBaseAsync();

        var customer = result.First();
        //Assert.Equal(Guid.Empty, customer.CustomerId);
        Assert.NotNull(customer.RelatedCustomer);

    }


    [Fact]
    public async Task IncludeBasic()
    {
        var query = sampleClient
            .Query
            .Orders()
            .Include(e => e.Nodes.Select(e => e.Customer.Orders))
            .Select(e => e.Nodes);

        var req = await query.GetRequestAsJsonAsync();


        var result = await query.ExecuteAsync();

        var order = result.First();
        Assert.NotEqual(Guid.Empty, order.OrderId);

        Assert.NotNull(order.Customer);
        Assert.Null(order.Customer.CustomerName);
        Assert.NotNull(order.Customer.Orders);
    }



    [Fact]
    public async Task IncludePrimitives_MultipleLevels()
    {
        var query = sampleClient
            .Query
            .Orders(1)
            .Include(e => e.Nodes.Select(e => e.Customer))
            .Include(e => e.Nodes.Select(e => e.Customer.Orders))
            .Select(e => e.Nodes);

        var req = await query.GetRequestAsync();


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
                Cust = new { n.Customer.CustomerName, n.Customer.Orders }
            }));

        var q = await query.GetRequestAsync();

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

    [Fact]
    public async Task IncludePrimitives_IncludeMultilevelComplex_PopulatePrimitivesOnMiddle()
    {
        var query = sampleClient
            .Query
            .Orders(where: new()
            {
                Customer = new() { CustomerId = new() { Eq = Guid.Parse("3b1761fb-6551-404e-80b0-a6c12f298a06") } }
            })
            .Select(e => e.Nodes.Select(n => new { n.Customer, n.Customer.Orders }))
            .ExecuteAsync();


        var order = (await query).First();

        Assert.NotNull(order.Customer.CustomerName);
    }
}