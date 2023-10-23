using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<ListFilterInputTypeOfOrderLineFilterInput>))]
public partial class ListFilterInputTypeOfOrderLineFilterInput : GraphInputBase
{
	[JsonPropertyName("all")]
	public OrderLineFilterInput All 
	{
		get => GetValue<OrderLineFilterInput>("all");
    	set => SetValue("all", value);
	}

	[JsonPropertyName("none")]
	public OrderLineFilterInput None 
	{
		get => GetValue<OrderLineFilterInput>("none");
    	set => SetValue("none", value);
	}

	[JsonPropertyName("some")]
	public OrderLineFilterInput Some 
	{
		get => GetValue<OrderLineFilterInput>("some");
    	set => SetValue("some", value);
	}

	[JsonPropertyName("any")]
	public bool? Any 
	{
		get => GetValue<bool?>("any");
    	set => SetValue("any", value);
	}

}