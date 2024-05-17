using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClient;

public partial class OrderLine : GraphQLTypeBase
{
    [JsonPropertyName("lineNumber")]
	public int LineNumber { get; set; }  

    [JsonPropertyName("order")]
	public Order Order { get; set; }  

    [JsonPropertyName("item")]
	public Item Item { get; set; }  

    [JsonPropertyName("price")]
	public decimal Price { get; set; }  

    [JsonPropertyName("quantity")]
	public double Quantity { get; set; }  

}
