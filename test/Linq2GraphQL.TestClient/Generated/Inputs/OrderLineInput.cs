using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<OrderLineInput>))]
public partial class OrderLineInput : GraphInputBase
{
	[JsonPropertyName("lineNumber")]
	public int LineNumber 
	{
		get => GetValue<int>("lineNumber");
    	set => SetValue("lineNumber", value);
	}

	[JsonPropertyName("order")]
	public OrderInput Order 
	{
		get => GetValue<OrderInput>("order");
    	set => SetValue("order", value);
	}

	[JsonPropertyName("item")]
	public ItemInput Item 
	{
		get => GetValue<ItemInput>("item");
    	set => SetValue("item", value);
	}

	[JsonPropertyName("price")]
	public decimal Price 
	{
		get => GetValue<decimal>("price");
    	set => SetValue("price", value);
	}

	[JsonPropertyName("quantity")]
	public float Quantity 
	{
		get => GetValue<float>("quantity");
    	set => SetValue("quantity", value);
	}

}