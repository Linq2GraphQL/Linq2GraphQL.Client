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

[JsonConverter(typeof(GraphInputConverter<ResolveConflictsLockedSeedConfig>))]
public partial class ResolveConflictsLockedSeedConfig : GraphInputBase
{
	[GraphQLMember("eventId")]
	[JsonPropertyName("eventId")]
	public ID EventId 
	{
		get => GetValue<ID>("eventId");
    	set => SetValue("eventId", value);
	}

	[GraphQLMember("numSeeds")]
	[JsonPropertyName("numSeeds")]
	public int NumSeeds 
	{
		get => GetValue<int>("numSeeds");
    	set => SetValue("numSeeds", value);
	}

}