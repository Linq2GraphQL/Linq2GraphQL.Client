using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<OrderLineFilterInput>))]
public partial class OrderLineFilterInput : GraphInputBase
{
	[JsonPropertyName("and")]
	public List<OrderLineFilterInput> And 
	{
		get => GetValue<List<OrderLineFilterInput>>("and");
    	set => SetValue("and", value);
	}

	[JsonPropertyName("or")]
	public List<OrderLineFilterInput> Or 
	{
		get => GetValue<List<OrderLineFilterInput>>("or");
    	set => SetValue("or", value);
	}

	[JsonPropertyName("lineNumber")]
	public IntOperationFilterInput LineNumber 
	{
		get => GetValue<IntOperationFilterInput>("lineNumber");
    	set => SetValue("lineNumber", value);
	}

	[JsonPropertyName("order")]
	public OrderFilterInput Order 
	{
		get => GetValue<OrderFilterInput>("order");
    	set => SetValue("order", value);
	}

	[JsonPropertyName("item")]
	public ItemFilterInput Item 
	{
		get => GetValue<ItemFilterInput>("item");
    	set => SetValue("item", value);
	}

	[JsonPropertyName("price")]
	public DecimalOperationFilterInput Price 
	{
		get => GetValue<DecimalOperationFilterInput>("price");
    	set => SetValue("price", value);
	}

	[JsonPropertyName("quantity")]
	public FloatOperationFilterInput Quantity 
	{
		get => GetValue<FloatOperationFilterInput>("quantity");
    	set => SetValue("quantity", value);
	}

}