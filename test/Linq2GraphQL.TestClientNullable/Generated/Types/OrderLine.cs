using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClientNullable;

public partial class OrderLine 
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
	public float Quantity { get; set; }  

}
