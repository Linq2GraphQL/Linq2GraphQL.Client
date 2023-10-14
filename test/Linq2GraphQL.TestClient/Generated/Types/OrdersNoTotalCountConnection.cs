using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

public partial class OrdersNoTotalCountConnection : Linq2GraphQL.Client.Common.ICursorPaging
{
	[JsonPropertyName("pageInfo")]
	public Linq2GraphQL.Client.Common.PageInfo PageInfo { get; set; }  

	[JsonPropertyName("edges")]
	public List<OrdersNoTotalCountEdge> Edges { get; set; }  

	[JsonPropertyName("nodes")]
	public List<Order> Nodes { get; set; }  


}
