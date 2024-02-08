using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<CustomerSortInput>))]
public partial class CustomerSortInput : GraphInputBase
{
	[JsonPropertyName("customerId")]
	public SortEnumType? CustomerId 
	{
		get => GetValue<SortEnumType?>("customerId");
    	set => SetValue("customerId", value);
	}

	[JsonPropertyName("customerName")]
	public SortEnumType? CustomerName 
	{
		get => GetValue<SortEnumType?>("customerName");
    	set => SetValue("customerName", value);
	}

	[JsonPropertyName("status")]
	public SortEnumType? Status 
	{
		get => GetValue<SortEnumType?>("status");
    	set => SetValue("status", value);
	}

	[JsonPropertyName("address")]
	public AddressSortInput Address 
	{
		get => GetValue<AddressSortInput>("address");
    	set => SetValue("address", value);
	}

}