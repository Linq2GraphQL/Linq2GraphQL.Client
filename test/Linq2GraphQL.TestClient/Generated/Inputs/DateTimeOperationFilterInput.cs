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

[JsonConverter(typeof(GraphInputConverter<DateTimeOperationFilterInput>))]
public partial class DateTimeOperationFilterInput : GraphInputBase
{
	[GraphQLMember("eq")]
	[JsonPropertyName("eq")]
	public DateTimeOffset? Eq 
	{
		get => GetValue<DateTimeOffset?>("eq");
    	set => SetValue("eq", value);
	}

	[GraphQLMember("neq")]
	[JsonPropertyName("neq")]
	public DateTimeOffset? Neq 
	{
		get => GetValue<DateTimeOffset?>("neq");
    	set => SetValue("neq", value);
	}

	[GraphQLMember("in")]
	[JsonPropertyName("in")]
	public List<DateTimeOffset?> In 
	{
		get => GetValue<List<DateTimeOffset?>>("in");
    	set => SetValue("in", value);
	}

	[GraphQLMember("nin")]
	[JsonPropertyName("nin")]
	public List<DateTimeOffset?> Nin 
	{
		get => GetValue<List<DateTimeOffset?>>("nin");
    	set => SetValue("nin", value);
	}

	[GraphQLMember("gt")]
	[JsonPropertyName("gt")]
	public DateTimeOffset? Gt 
	{
		get => GetValue<DateTimeOffset?>("gt");
    	set => SetValue("gt", value);
	}

	[GraphQLMember("ngt")]
	[JsonPropertyName("ngt")]
	public DateTimeOffset? Ngt 
	{
		get => GetValue<DateTimeOffset?>("ngt");
    	set => SetValue("ngt", value);
	}

	[GraphQLMember("gte")]
	[JsonPropertyName("gte")]
	public DateTimeOffset? Gte 
	{
		get => GetValue<DateTimeOffset?>("gte");
    	set => SetValue("gte", value);
	}

	[GraphQLMember("ngte")]
	[JsonPropertyName("ngte")]
	public DateTimeOffset? Ngte 
	{
		get => GetValue<DateTimeOffset?>("ngte");
    	set => SetValue("ngte", value);
	}

	[GraphQLMember("lt")]
	[JsonPropertyName("lt")]
	public DateTimeOffset? Lt 
	{
		get => GetValue<DateTimeOffset?>("lt");
    	set => SetValue("lt", value);
	}

	[GraphQLMember("nlt")]
	[JsonPropertyName("nlt")]
	public DateTimeOffset? Nlt 
	{
		get => GetValue<DateTimeOffset?>("nlt");
    	set => SetValue("nlt", value);
	}

	[GraphQLMember("lte")]
	[JsonPropertyName("lte")]
	public DateTimeOffset? Lte 
	{
		get => GetValue<DateTimeOffset?>("lte");
    	set => SetValue("lte", value);
	}

	[GraphQLMember("nlte")]
	[JsonPropertyName("nlte")]
	public DateTimeOffset? Nlte 
	{
		get => GetValue<DateTimeOffset?>("nlte");
    	set => SetValue("nlte", value);
	}

}