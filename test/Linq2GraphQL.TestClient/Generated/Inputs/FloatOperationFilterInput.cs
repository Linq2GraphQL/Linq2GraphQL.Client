using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<FloatOperationFilterInput>))]
public partial class FloatOperationFilterInput : GraphInputBase
{
	[JsonPropertyName("eq")]
	public float? Eq 
	{
		get => GetValue<float?>("eq");
    	set => SetValue("eq", value);
	}

	[JsonPropertyName("neq")]
	public float? Neq 
	{
		get => GetValue<float?>("neq");
    	set => SetValue("neq", value);
	}

	[JsonPropertyName("in")]
	public List<float?> In 
	{
		get => GetValue<List<float?>>("in");
    	set => SetValue("in", value);
	}

	[JsonPropertyName("nin")]
	public List<float?> Nin 
	{
		get => GetValue<List<float?>>("nin");
    	set => SetValue("nin", value);
	}

	[JsonPropertyName("gt")]
	public float? Gt 
	{
		get => GetValue<float?>("gt");
    	set => SetValue("gt", value);
	}

	[JsonPropertyName("ngt")]
	public float? Ngt 
	{
		get => GetValue<float?>("ngt");
    	set => SetValue("ngt", value);
	}

	[JsonPropertyName("gte")]
	public float? Gte 
	{
		get => GetValue<float?>("gte");
    	set => SetValue("gte", value);
	}

	[JsonPropertyName("ngte")]
	public float? Ngte 
	{
		get => GetValue<float?>("ngte");
    	set => SetValue("ngte", value);
	}

	[JsonPropertyName("lt")]
	public float? Lt 
	{
		get => GetValue<float?>("lt");
    	set => SetValue("lt", value);
	}

	[JsonPropertyName("nlt")]
	public float? Nlt 
	{
		get => GetValue<float?>("nlt");
    	set => SetValue("nlt", value);
	}

	[JsonPropertyName("lte")]
	public float? Lte 
	{
		get => GetValue<float?>("lte");
    	set => SetValue("lte", value);
	}

	[JsonPropertyName("nlte")]
	public float? Nlte 
	{
		get => GetValue<float?>("nlte");
    	set => SetValue("nlte", value);
	}

}