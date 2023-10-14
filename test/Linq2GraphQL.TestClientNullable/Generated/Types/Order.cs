using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClientNullable;

public partial class Order 
{
	[JsonPropertyName("orderId")]
	public Guid OrderId { get; set; }  
	[JsonPropertyName("customer")]
	public Customer Customer { get; set; }  
	[JsonPropertyName("address")]
	public Address Address { get; set; }  
	[JsonPropertyName("orderDate")]
	public DateTimeOffset OrderDate { get; set; }  
	[JsonPropertyName("lines")]
	public List<OrderLine> Lines { get; set; }  

}
