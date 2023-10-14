using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

public partial class Customer 
{
	[JsonPropertyName("customerId")]
	public Guid CustomerId { get; set; }  

	[JsonPropertyName("customerName")]
	public string CustomerName { get; set; }  

	[JsonPropertyName("status")]
	public CustomerStatus Status { get; set; }  

	[JsonPropertyName("orders")]
	public List<Order> Orders { get; set; }  


}
