using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<CustomerFilterInput>))]
public partial class CustomerFilterInput : GraphInputBase
{
	[JsonPropertyName("and")]
	public List<CustomerFilterInput> And 
	{
		get => GetValue<List<CustomerFilterInput>>("and");
    	set => SetValue("and", value);
	}

	[JsonPropertyName("or")]
	public List<CustomerFilterInput> Or 
	{
		get => GetValue<List<CustomerFilterInput>>("or");
    	set => SetValue("or", value);
	}

	[JsonPropertyName("customerId")]
	public UuidOperationFilterInput CustomerId 
	{
		get => GetValue<UuidOperationFilterInput>("customerId");
    	set => SetValue("customerId", value);
	}

	[JsonPropertyName("customerName")]
	public StringOperationFilterInput CustomerName 
	{
		get => GetValue<StringOperationFilterInput>("customerName");
    	set => SetValue("customerName", value);
	}

	[JsonPropertyName("status")]
	public CustomerStatusOperationFilterInput Status 
	{
		get => GetValue<CustomerStatusOperationFilterInput>("status");
    	set => SetValue("status", value);
	}

	[JsonPropertyName("orders")]
	public ListFilterInputTypeOfOrderFilterInput Orders 
	{
		get => GetValue<ListFilterInputTypeOfOrderFilterInput>("orders");
    	set => SetValue("orders", value);
	}

}