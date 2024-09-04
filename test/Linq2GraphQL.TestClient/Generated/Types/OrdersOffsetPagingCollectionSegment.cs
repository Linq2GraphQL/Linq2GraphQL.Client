using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClient;


public partial class OrdersOffsetPagingCollectionSegment : GraphQLTypeBase
{
    [JsonPropertyName("pageInfo")]
	public CollectionSegmentInfo PageInfo { get; set; }  

    [JsonPropertyName("items")]
	public List<Order> Items { get; set; }  

    [JsonPropertyName("totalCount")]
	public int TotalCount { get; set; }  

}
