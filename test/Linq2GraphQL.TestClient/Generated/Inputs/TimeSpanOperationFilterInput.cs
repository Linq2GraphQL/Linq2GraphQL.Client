using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<TimeSpanOperationFilterInput>))]
public partial class TimeSpanOperationFilterInput : GraphInputBase
{
	[JsonPropertyName("eq")]
	public TimeSpan? Eq 
	{
		get => GetValue<TimeSpan?>("eq");
    	set => SetValue("eq", value);
	}

	[JsonPropertyName("neq")]
	public TimeSpan? Neq 
	{
		get => GetValue<TimeSpan?>("neq");
    	set => SetValue("neq", value);
	}

	[JsonPropertyName("in")]
	public List<TimeSpan?> In 
	{
		get => GetValue<List<TimeSpan?>>("in");
    	set => SetValue("in", value);
	}

	[JsonPropertyName("nin")]
	public List<TimeSpan?> Nin 
	{
		get => GetValue<List<TimeSpan?>>("nin");
    	set => SetValue("nin", value);
	}

	[JsonPropertyName("gt")]
	public TimeSpan? Gt 
	{
		get => GetValue<TimeSpan?>("gt");
    	set => SetValue("gt", value);
	}

	[JsonPropertyName("ngt")]
	public TimeSpan? Ngt 
	{
		get => GetValue<TimeSpan?>("ngt");
    	set => SetValue("ngt", value);
	}

	[JsonPropertyName("gte")]
	public TimeSpan? Gte 
	{
		get => GetValue<TimeSpan?>("gte");
    	set => SetValue("gte", value);
	}

	[JsonPropertyName("ngte")]
	public TimeSpan? Ngte 
	{
		get => GetValue<TimeSpan?>("ngte");
    	set => SetValue("ngte", value);
	}

	[JsonPropertyName("lt")]
	public TimeSpan? Lt 
	{
		get => GetValue<TimeSpan?>("lt");
    	set => SetValue("lt", value);
	}

	[JsonPropertyName("nlt")]
	public TimeSpan? Nlt 
	{
		get => GetValue<TimeSpan?>("nlt");
    	set => SetValue("nlt", value);
	}

	[JsonPropertyName("lte")]
	public TimeSpan? Lte 
	{
		get => GetValue<TimeSpan?>("lte");
    	set => SetValue("lte", value);
	}

	[JsonPropertyName("nlte")]
	public TimeSpan? Nlte 
	{
		get => GetValue<TimeSpan?>("nlte");
    	set => SetValue("nlte", value);
	}

}