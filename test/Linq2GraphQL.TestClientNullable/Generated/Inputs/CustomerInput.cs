using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClientNullable;

[JsonConverter(typeof(GraphInputConverter<CustomerInput>))]
public partial class CustomerInput : GraphInputBase
{
	[JsonPropertyName("customerId")]
	public Guid CustomerId 
	{
		get => GetValue<Guid>("customerId");
    	set => SetValue("customerId", value);
	}

	[JsonPropertyName("customerName")]
	public string CustomerName 
	{
		get => GetValue<string>("customerName");
    	set => SetValue("customerName", value);
	}

	[JsonPropertyName("status")]
	public CustomerStatus Status 
	{
		get => GetValue<CustomerStatus>("status");
    	set => SetValue("status", value);
	}

	[JsonPropertyName("orders")]
	public List<OrderInput> Orders 
	{
		get => GetValue<List<OrderInput>>("orders");
    	set => SetValue("orders", value);
	}

	[JsonPropertyName("address")]
	public AddressInput? Address 
	{
		get => GetValue<AddressInput?>("address");
    	set => SetValue("address", value);
	}

}