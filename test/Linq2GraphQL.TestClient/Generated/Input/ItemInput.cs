using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<ItemInput>))]
public partial class ItemInput : GraphInputBase
{
	[JsonPropertyName("itemId")]
	public string ItemId 
	{
		get => GetValue<string>("itemId");
    	set => SetValue("itemId", value);
	}

	[JsonPropertyName("itemName")]
	public string ItemName 
	{
		get => GetValue<string>("itemName");
    	set => SetValue("itemName", value);
	}

}