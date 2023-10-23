using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<OrderSortInput>))]
public partial class OrderSortInput : GraphInputBase
{
	[JsonPropertyName("orderId")]
	public SortEnumType? OrderId 
	{
		get => GetValue<SortEnumType?>("orderId");
    	set => SetValue("orderId", value);
	}

	[JsonPropertyName("customer")]
	public CustomerSortInput Customer 
	{
		get => GetValue<CustomerSortInput>("customer");
    	set => SetValue("customer", value);
	}

	[JsonPropertyName("address")]
	public AddressSortInput Address 
	{
		get => GetValue<AddressSortInput>("address");
    	set => SetValue("address", value);
	}

	[JsonPropertyName("orderDate")]
	public SortEnumType? OrderDate 
	{
		get => GetValue<SortEnumType?>("orderDate");
    	set => SetValue("orderDate", value);
	}

}