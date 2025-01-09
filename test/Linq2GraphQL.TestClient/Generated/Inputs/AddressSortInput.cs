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

[JsonConverter(typeof(GraphInputConverter<AddressSortInput>))]
public partial class AddressSortInput : GraphInputBase
{
	[GraphQLMember("name")]
	public SortEnumType? Name 
	{
		get => GetValue<SortEnumType?>("name");
    	set => SetValue("name", value);
	}

	[GraphQLMember("street")]
	public SortEnumType? Street 
	{
		get => GetValue<SortEnumType?>("street");
    	set => SetValue("street", value);
	}

	[GraphQLMember("postalCode")]
	public SortEnumType? PostalCode 
	{
		get => GetValue<SortEnumType?>("postalCode");
    	set => SetValue("postalCode", value);
	}

}