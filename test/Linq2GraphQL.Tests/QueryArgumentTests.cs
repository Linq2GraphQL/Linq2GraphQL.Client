﻿using Linq2GraphQL.TestClient;

namespace Linq2GraphQL.Tests;

public class QueryArgumentTests : IClassFixture<SampleClientFixture>
{
    private readonly SampleClient sampleClient;

    public QueryArgumentTests(SampleClientFixture safeModeClient)
    {
        sampleClient = safeModeClient.sampleClient;
    }

    [Fact]
    public async Task TopAndInQueryArguments_SameName()
    {
        var query = sampleClient
            .Query
            .Orders(1)
            .Include(e => e.Nodes.Select(e => e.OrderHello("JOcke", 2)))
            .Include(e => e.Nodes.Select(e => e.OrderAddress(AddressType.Invoice)))
            .Select(e => e.Nodes);

        var request = await query.GetRequestAsync();
        var result = await query.ExecuteAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task TopLevelArguments()
    {
        var query = sampleClient
            .Query
            .Orders(1)
            .Select(e => e.Nodes);

        var request = await query.GetRequestAsync();
        var result = await query.ExecuteAsync();

        var order = result.First();

        var address = order.OrderAddress;

        Assert.Single(result);
    }

    [Fact]
    public async Task InQueryArguments()
    {
        var query = sampleClient
            .Query
            .Orders(1)
            .Include(e => e.Nodes.Select(e => e.OrderAddress(AddressType.Invoice)))
            .Select(e => e.Nodes);

        var request = query.GetRequestAsync();

        var result = await query.ExecuteAsync();

        var order = result.First();

        Assert.NotNull(order.OrderAddress);
        Assert.Equal("Invoice", result.First().OrderAddress?.Name);
    }

    [Fact]
    public async Task InQueryAnonymousType()
    {
        var query = sampleClient
            .Query
            .Orders(2)
            .Include(e => e.Nodes)
            .Select(e => e.Nodes.Select(e => e.OrderHello("Kalle", 123)));

        var gQ = await query.GetRequestAsync();

        var node = query.QueryNode;
        var result = await query.ExecuteAsync();

        Assert.NotNull(result.First());
    }

    [Fact]
    public async Task InQueryArguments_Multiple()
    {
        var query = sampleClient
            .Query
            .Orders()
            .Select(e => e.Nodes.Select(f => new
                {
                    Jocke = f.OrderHello("Jocke", 22), Kalle = f.OrderHello("Kalle", 11)
                })
            );

        var request = query.GetRequestAsync();
        var result = await query.ExecuteAsync();

        var addresses = result.First();

        Assert.Equal("Hello, Jocke [22]", addresses.Jocke);
        Assert.Equal("Hello, Kalle [11]", addresses.Kalle);
    }
}