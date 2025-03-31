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

namespace StartGG.Client;

[JsonConverter(typeof(GraphInputConverter<StationFilter>))]
public partial class StationFilter : GraphInputBase
{
	[GraphQLMember("page")]
	[JsonPropertyName("page")]
	public int? Page 
	{
		get => GetValue<int?>("page");
    	set => SetValue("page", value);
	}

	[GraphQLMember("perPage")]
	[JsonPropertyName("perPage")]
	public int? PerPage 
	{
		get => GetValue<int?>("perPage");
    	set => SetValue("perPage", value);
	}

}