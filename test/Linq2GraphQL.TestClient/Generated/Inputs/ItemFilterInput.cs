using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<ItemFilterInput>))]
public partial class ItemFilterInput : GraphInputBase
{
	[JsonPropertyName("and")]
	public List<ItemFilterInput> And 
	{
		get => GetValue<List<ItemFilterInput>>("and");
    	set => SetValue("and", value);
	}

	[JsonPropertyName("or")]
	public List<ItemFilterInput> Or 
	{
		get => GetValue<List<ItemFilterInput>>("or");
    	set => SetValue("or", value);
	}

	[JsonPropertyName("itemId")]
	public StringOperationFilterInput ItemId 
	{
		get => GetValue<StringOperationFilterInput>("itemId");
    	set => SetValue("itemId", value);
	}

	[JsonPropertyName("itemName")]
	public StringOperationFilterInput ItemName 
	{
		get => GetValue<StringOperationFilterInput>("itemName");
    	set => SetValue("itemName", value);
	}

}