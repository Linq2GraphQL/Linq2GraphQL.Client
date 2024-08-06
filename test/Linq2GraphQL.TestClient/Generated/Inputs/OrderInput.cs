using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<OrderInput>))]
public partial class OrderInput : GraphInputBase
{
	[JsonPropertyName("orderId")]
	public Guid OrderId 
	{
		get => GetValue<Guid>("orderId");
    	set => SetValue("orderId", value);
	}

	[JsonPropertyName("customer")]
	public CustomerInput Customer 
	{
		get => GetValue<CustomerInput>("customer");
    	set => SetValue("customer", value);
	}

	[JsonPropertyName("address")]
	public AddressInput Address 
	{
		get => GetValue<AddressInput>("address");
    	set => SetValue("address", value);
	}

	[JsonPropertyName("orderDate")]
	public DateTimeOffset OrderDate 
	{
		get => GetValue<DateTimeOffset>("orderDate");
    	set => SetValue("orderDate", value);
	}

	[JsonPropertyName("lines")]
	public List<OrderLineInput> Lines 
	{
		get => GetValue<List<OrderLineInput>>("lines");
    	set => SetValue("lines", value);
	}

	[JsonPropertyName("entryTime")]
	public TimeSpan? EntryTime 
	{
		get => GetValue<TimeSpan?>("entryTime");
    	set => SetValue("entryTime", value);
	}

}