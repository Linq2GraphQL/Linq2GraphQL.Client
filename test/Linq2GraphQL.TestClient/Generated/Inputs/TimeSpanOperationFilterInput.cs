//---------------------------------------------------------------------
// This code was automatically generated by Linq2GraphQL
// Please don't edit this file
// Github:https://github.com/linq2graphql/linq2graphql.client
// Url: https://linq2graphql.com
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<TimeSpanOperationFilterInput>))]
public partial class TimeSpanOperationFilterInput : GraphInputBase
{
	[GraphQLMember("eq")]
	[JsonPropertyName("eq")]
	public TimeSpan? Eq 
	{
		get => GetValue<TimeSpan?>("eq");
    	set => SetValue("eq", value);
	}

	[GraphQLMember("neq")]
	[JsonPropertyName("neq")]
	public TimeSpan? Neq 
	{
		get => GetValue<TimeSpan?>("neq");
    	set => SetValue("neq", value);
	}

	[GraphQLMember("in")]
	[JsonPropertyName("in")]
	public List<TimeSpan?> In 
	{
		get => GetValue<List<TimeSpan?>>("in");
    	set => SetValue("in", value);
	}

	[GraphQLMember("nin")]
	[JsonPropertyName("nin")]
	public List<TimeSpan?> Nin 
	{
		get => GetValue<List<TimeSpan?>>("nin");
    	set => SetValue("nin", value);
	}

	[GraphQLMember("gt")]
	[JsonPropertyName("gt")]
	public TimeSpan? Gt 
	{
		get => GetValue<TimeSpan?>("gt");
    	set => SetValue("gt", value);
	}

	[GraphQLMember("ngt")]
	[JsonPropertyName("ngt")]
	public TimeSpan? Ngt 
	{
		get => GetValue<TimeSpan?>("ngt");
    	set => SetValue("ngt", value);
	}

	[GraphQLMember("gte")]
	[JsonPropertyName("gte")]
	public TimeSpan? Gte 
	{
		get => GetValue<TimeSpan?>("gte");
    	set => SetValue("gte", value);
	}

	[GraphQLMember("ngte")]
	[JsonPropertyName("ngte")]
	public TimeSpan? Ngte 
	{
		get => GetValue<TimeSpan?>("ngte");
    	set => SetValue("ngte", value);
	}

	[GraphQLMember("lt")]
	[JsonPropertyName("lt")]
	public TimeSpan? Lt 
	{
		get => GetValue<TimeSpan?>("lt");
    	set => SetValue("lt", value);
	}

	[GraphQLMember("nlt")]
	[JsonPropertyName("nlt")]
	public TimeSpan? Nlt 
	{
		get => GetValue<TimeSpan?>("nlt");
    	set => SetValue("nlt", value);
	}

	[GraphQLMember("lte")]
	[JsonPropertyName("lte")]
	public TimeSpan? Lte 
	{
		get => GetValue<TimeSpan?>("lte");
    	set => SetValue("lte", value);
	}

	[GraphQLMember("nlte")]
	[JsonPropertyName("nlte")]
	public TimeSpan? Nlte 
	{
		get => GetValue<TimeSpan?>("nlte");
    	set => SetValue("nlte", value);
	}

}