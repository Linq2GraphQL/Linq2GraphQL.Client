using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClientNullable;

#pragma warning disable CS8618

public partial class Customer : GraphQLTypeBase
{
    [JsonPropertyName("customerId")]
	public Guid CustomerId { get; set; }  

    [JsonPropertyName("customerName")]
	public string CustomerName { get; set; }  

    [JsonPropertyName("status")]
	public CustomerStatus Status { get; set; }  

    [JsonPropertyName("orders")]
	public List<Order> Orders { get; set; }  

    [JsonPropertyName("address")]
	public Address? Address { get; set; }  

}
