//---------------------------------------------------------------------
// This code was automatically generated by Linq2GraphQL
// Please don't edit this file
// Github:https://github.com/linq2graphql/linq2graphql.client
// Url: https://linq2graphql.com
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<OrderInput>))]
public partial class OrderInput : GraphInputBase
{
	[GraphQLMember("orderId")]
	[JsonPropertyName("orderId")]
	public Guid OrderId 
	{
		get => GetValue<Guid>("orderId");
    	set => SetValue("orderId", value);
	}

	[GraphQLMember("customer")]
	[JsonPropertyName("customer")]
	public CustomerInput Customer 
	{
		get => GetValue<CustomerInput>("customer");
    	set => SetValue("customer", value);
	}

	[GraphQLMember("address")]
	[JsonPropertyName("address")]
	public AddressInput Address 
	{
		get => GetValue<AddressInput>("address");
    	set => SetValue("address", value);
	}

	[GraphQLMember("orderDate")]
	[JsonPropertyName("orderDate")]
	public DateTimeOffset OrderDate 
	{
		get => GetValue<DateTimeOffset>("orderDate");
    	set => SetValue("orderDate", value);
	}

	[GraphQLMember("lines")]
	[JsonPropertyName("lines")]
	public List<OrderLineInput> Lines 
	{
		get => GetValue<List<OrderLineInput>>("lines");
    	set => SetValue("lines", value);
	}

	[GraphQLMember("entryTime")]
	[JsonPropertyName("entryTime")]
	public TimeSpan? EntryTime 
	{
		get => GetValue<TimeSpan?>("entryTime");
    	set => SetValue("entryTime", value);
	}

    [Obsolete("This propery is obsolete and should not be used!")]
	[GraphQLMember("grade")]
	[JsonPropertyName("grade")]
	public string Grade 
	{
		get => GetValue<string>("grade");
    	set => SetValue("grade", value);
	}

}