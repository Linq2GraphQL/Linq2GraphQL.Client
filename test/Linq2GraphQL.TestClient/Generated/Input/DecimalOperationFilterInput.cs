using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<DecimalOperationFilterInput>))]
public partial class DecimalOperationFilterInput : GraphInputBase
{
	[JsonPropertyName("eq")]
	public decimal? Eq 
	{
		get => GetValue<decimal?>("eq");
    	set => SetValue("eq", value);
	}

	[JsonPropertyName("neq")]
	public decimal? Neq 
	{
		get => GetValue<decimal?>("neq");
    	set => SetValue("neq", value);
	}

	[JsonPropertyName("in")]
	public List<decimal?> In 
	{
		get => GetValue<List<decimal?>>("in");
    	set => SetValue("in", value);
	}

	[JsonPropertyName("nin")]
	public List<decimal?> Nin 
	{
		get => GetValue<List<decimal?>>("nin");
    	set => SetValue("nin", value);
	}

	[JsonPropertyName("gt")]
	public decimal? Gt 
	{
		get => GetValue<decimal?>("gt");
    	set => SetValue("gt", value);
	}

	[JsonPropertyName("ngt")]
	public decimal? Ngt 
	{
		get => GetValue<decimal?>("ngt");
    	set => SetValue("ngt", value);
	}

	[JsonPropertyName("gte")]
	public decimal? Gte 
	{
		get => GetValue<decimal?>("gte");
    	set => SetValue("gte", value);
	}

	[JsonPropertyName("ngte")]
	public decimal? Ngte 
	{
		get => GetValue<decimal?>("ngte");
    	set => SetValue("ngte", value);
	}

	[JsonPropertyName("lt")]
	public decimal? Lt 
	{
		get => GetValue<decimal?>("lt");
    	set => SetValue("lt", value);
	}

	[JsonPropertyName("nlt")]
	public decimal? Nlt 
	{
		get => GetValue<decimal?>("nlt");
    	set => SetValue("nlt", value);
	}

	[JsonPropertyName("lte")]
	public decimal? Lte 
	{
		get => GetValue<decimal?>("lte");
    	set => SetValue("lte", value);
	}

	[JsonPropertyName("nlte")]
	public decimal? Nlte 
	{
		get => GetValue<decimal?>("nlte");
    	set => SetValue("nlte", value);
	}

}