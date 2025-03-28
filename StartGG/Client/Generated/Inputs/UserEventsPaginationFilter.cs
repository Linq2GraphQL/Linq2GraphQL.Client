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

[JsonConverter(typeof(GraphInputConverter<UserEventsPaginationFilter>))]
public partial class UserEventsPaginationFilter : GraphInputBase
{
	[GraphQLMember("videogameId")]
	[JsonPropertyName("videogameId")]
	public List<ID> VideogameId 
	{
		get => GetValue<List<ID>>("videogameId");
    	set => SetValue("videogameId", value);
	}

	[GraphQLMember("eventType")]
	[JsonPropertyName("eventType")]
	public int? EventType 
	{
		get => GetValue<int?>("eventType");
    	set => SetValue("eventType", value);
	}

	[GraphQLMember("minEntrantCount")]
	[JsonPropertyName("minEntrantCount")]
	public int? MinEntrantCount 
	{
		get => GetValue<int?>("minEntrantCount");
    	set => SetValue("minEntrantCount", value);
	}

	[GraphQLMember("maxEntrantCount")]
	[JsonPropertyName("maxEntrantCount")]
	public int? MaxEntrantCount 
	{
		get => GetValue<int?>("maxEntrantCount");
    	set => SetValue("maxEntrantCount", value);
	}

	[GraphQLMember("location")]
	[JsonPropertyName("location")]
	public LocationFilterType Location 
	{
		get => GetValue<LocationFilterType>("location");
    	set => SetValue("location", value);
	}

	[GraphQLMember("search")]
	[JsonPropertyName("search")]
	public PaginationSearchType Search 
	{
		get => GetValue<PaginationSearchType>("search");
    	set => SetValue("search", value);
	}

}