using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClient;

public partial class OrdersNoBackwardPaginationConnection : GraphQLTypeBase, Linq2GraphQL.Client.Common.ICursorPaging
{
    [JsonPropertyName("pageInfo")]
	public Linq2GraphQL.Client.Common.PageInfo PageInfo { get; set; }  

    [JsonPropertyName("edges")]
	public List<OrdersNoBackwardPaginationEdge> Edges { get; set; }  

    [JsonPropertyName("nodes")]
	public List<Order> Nodes { get; set; }  

    [JsonPropertyName("totalCount")]
	public int TotalCount { get; set; }  

}
