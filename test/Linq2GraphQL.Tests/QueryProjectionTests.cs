﻿using Linq2GraphQL.TestClient;

namespace Linq2GraphQL.Tests;

public class QueryProjectionTests : IClassFixture<SampleClientFixture>
{
    private readonly SampleClient sampleClient;

    public QueryProjectionTests(SampleClientFixture safeModeClient)
    {
        sampleClient = safeModeClient.sampleClient;
    }

    [Fact]
    public async Task ProjectToAnonumous()
    {
        //var query = sampleClient
        //    .Query
        //    .Orders()
        //    .Select(e => e.Nodes.Select(o => new { o.OrderId, o.Address, Hello = o.OrderHello("Kalle", 1), e.TotalCount }));

        var query = sampleClient
          .Query
          .Orders()
          .Select(e => e.Nodes.Select(o => new {  e.TotalCount }));

        var request = await query.GetRequestAsJsonAsync();
        var result = await query.ExecuteAsync();


        //Assert.NotEqual(Guid.Empty, result.First().OrderId);
        //Assert.NotNull(result.First().Address);

        //var baseOrder = query.BaseResult.Nodes.First();
        //Assert.Null(baseOrder.Customer);
        //Assert.Equal(DateTimeOffset.MinValue, baseOrder.OrderDate);
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

    [Fact]
    public async Task Project_SelectMany()
    {
        var query = sampleClient
            .Query
            .Customers()
            .Select(e => e.SelectMany(e => e.Orders));

        var result = await query.ExecuteAsync();


        Assert.True(result.All(e => e.OrderId != default));
    }


    [Fact]
    public async Task Project_TopLevelAllPrimitive()
    {
        var query = sampleClient
            .Query
            .Customers()
            .Include()
            .Select(e => new { Customers = e, Orders = e.SelectMany(f => f.Orders) });

        var request = await query.GetRequestAsync();
        var result = await query.ExecuteAsync();
        var t = result.Customers.All(e => e.CustomerId != default);

        Assert.True(result.Customers.All(e => e.CustomerId != default));
    }
}

public class OrderIdAddress
{
    public Guid OrderId { get; set; }
    public Address? Address { get; set; }

    public string? Hello { get; set; }

    private int? MyCount { get; set; }
}