using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

public partial class OrdersOffsetPagingCollectionSegment 
{
	[JsonPropertyName("pageInfo")]
	public CollectionSegmentInfo PageInfo { get; set; }  

	[JsonPropertyName("items")]
	public List<Order> Items { get; set; }  

	[JsonPropertyName("totalCount")]
	public int TotalCount { get; set; }  


}
