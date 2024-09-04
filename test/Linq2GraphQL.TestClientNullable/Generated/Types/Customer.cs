using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClientNullable;

public partial class Customer : GraphQLTypeBase
{
    [JsonPropertyName("customerId")]
	public required Guid CustomerId { get; set; }  

    [JsonPropertyName("customerName")]
	public required string CustomerName { get; set; }  

    [JsonPropertyName("status")]
	public required CustomerStatus Status { get; set; }  

    [JsonPropertyName("orders")]
	public required List<Order> Orders { get; set; }  

    [JsonPropertyName("address")]
	public Address? Address { get; set; }  

}
