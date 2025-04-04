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

[JsonConverter(typeof(GraphInputConverter<IAnimalSortInput>))]
public partial class IAnimalSortInput : GraphInputBase
{
	[GraphQLMember("name")]
	[JsonPropertyName("name")]
	public SortEnumType? Name 
	{
		get => GetValue<SortEnumType?>("name");
    	set => SetValue("name", value);
	}

	[GraphQLMember("numberOfLegs")]
	[JsonPropertyName("numberOfLegs")]
	public SortEnumType? NumberOfLegs 
	{
		get => GetValue<SortEnumType?>("numberOfLegs");
    	set => SetValue("numberOfLegs", value);
	}

}