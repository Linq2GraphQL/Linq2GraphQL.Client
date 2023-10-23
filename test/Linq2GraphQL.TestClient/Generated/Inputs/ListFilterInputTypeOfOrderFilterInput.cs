using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<ListFilterInputTypeOfOrderFilterInput>))]
public partial class ListFilterInputTypeOfOrderFilterInput : GraphInputBase
{
	[JsonPropertyName("all")]
	public OrderFilterInput All 
	{
		get => GetValue<OrderFilterInput>("all");
    	set => SetValue("all", value);
	}

	[JsonPropertyName("none")]
	public OrderFilterInput None 
	{
		get => GetValue<OrderFilterInput>("none");
    	set => SetValue("none", value);
	}

	[JsonPropertyName("some")]
	public OrderFilterInput Some 
	{
		get => GetValue<OrderFilterInput>("some");
    	set => SetValue("some", value);
	}

	[JsonPropertyName("any")]
	public bool? Any 
	{
		get => GetValue<bool?>("any");
    	set => SetValue("any", value);
	}

}