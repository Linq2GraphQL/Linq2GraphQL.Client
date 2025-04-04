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

[JsonConverter(typeof(GraphInputConverter<UuidOperationFilterInput>))]
public partial class UuidOperationFilterInput : GraphInputBase
{
	[GraphQLMember("eq")]
	[JsonPropertyName("eq")]
	public Guid? Eq 
	{
		get => GetValue<Guid?>("eq");
    	set => SetValue("eq", value);
	}

	[GraphQLMember("neq")]
	[JsonPropertyName("neq")]
	public Guid? Neq 
	{
		get => GetValue<Guid?>("neq");
    	set => SetValue("neq", value);
	}

	[GraphQLMember("in")]
	[JsonPropertyName("in")]
	public List<Guid?> In 
	{
		get => GetValue<List<Guid?>>("in");
    	set => SetValue("in", value);
	}

	[GraphQLMember("nin")]
	[JsonPropertyName("nin")]
	public List<Guid?> Nin 
	{
		get => GetValue<List<Guid?>>("nin");
    	set => SetValue("nin", value);
	}

	[GraphQLMember("gt")]
	[JsonPropertyName("gt")]
	public Guid? Gt 
	{
		get => GetValue<Guid?>("gt");
    	set => SetValue("gt", value);
	}

	[GraphQLMember("ngt")]
	[JsonPropertyName("ngt")]
	public Guid? Ngt 
	{
		get => GetValue<Guid?>("ngt");
    	set => SetValue("ngt", value);
	}

	[GraphQLMember("gte")]
	[JsonPropertyName("gte")]
	public Guid? Gte 
	{
		get => GetValue<Guid?>("gte");
    	set => SetValue("gte", value);
	}

	[GraphQLMember("ngte")]
	[JsonPropertyName("ngte")]
	public Guid? Ngte 
	{
		get => GetValue<Guid?>("ngte");
    	set => SetValue("ngte", value);
	}

	[GraphQLMember("lt")]
	[JsonPropertyName("lt")]
	public Guid? Lt 
	{
		get => GetValue<Guid?>("lt");
    	set => SetValue("lt", value);
	}

	[GraphQLMember("nlt")]
	[JsonPropertyName("nlt")]
	public Guid? Nlt 
	{
		get => GetValue<Guid?>("nlt");
    	set => SetValue("nlt", value);
	}

	[GraphQLMember("lte")]
	[JsonPropertyName("lte")]
	public Guid? Lte 
	{
		get => GetValue<Guid?>("lte");
    	set => SetValue("lte", value);
	}

	[GraphQLMember("nlte")]
	[JsonPropertyName("nlte")]
	public Guid? Nlte 
	{
		get => GetValue<Guid?>("nlte");
    	set => SetValue("nlte", value);
	}

}