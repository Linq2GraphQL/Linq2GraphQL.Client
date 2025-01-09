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

[JsonConverter(typeof(GraphInputConverter<CustomerFilterInput>))]
public partial class CustomerFilterInput : GraphInputBase
{
	[GraphQLMember("and")]
	[JsonPropertyName("and")]
	public List<CustomerFilterInput> And 
	{
		get => GetValue<List<CustomerFilterInput>>("and");
    	set => SetValue("and", value);
	}

	[GraphQLMember("or")]
	[JsonPropertyName("or")]
	public List<CustomerFilterInput> Or 
	{
		get => GetValue<List<CustomerFilterInput>>("or");
    	set => SetValue("or", value);
	}

	[GraphQLMember("customerId")]
	[JsonPropertyName("customerId")]
	public UuidOperationFilterInput CustomerId 
	{
		get => GetValue<UuidOperationFilterInput>("customerId");
    	set => SetValue("customerId", value);
	}

	[GraphQLMember("customerName")]
	[JsonPropertyName("customerName")]
	public StringOperationFilterInput CustomerName 
	{
		get => GetValue<StringOperationFilterInput>("customerName");
    	set => SetValue("customerName", value);
	}

	[GraphQLMember("status")]
	[JsonPropertyName("status")]
	public CustomerStatusOperationFilterInput Status 
	{
		get => GetValue<CustomerStatusOperationFilterInput>("status");
    	set => SetValue("status", value);
	}

	[GraphQLMember("orders")]
	[JsonPropertyName("orders")]
	public ListFilterInputTypeOfOrderFilterInput Orders 
	{
		get => GetValue<ListFilterInputTypeOfOrderFilterInput>("orders");
    	set => SetValue("orders", value);
	}

	[GraphQLMember("address")]
	[JsonPropertyName("address")]
	public AddressFilterInput Address 
	{
		get => GetValue<AddressFilterInput>("address");
    	set => SetValue("address", value);
	}

}