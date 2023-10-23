using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<AddressSortInput>))]
public partial class AddressSortInput : GraphInputBase
{
	[JsonPropertyName("name")]
	public SortEnumType? Name 
	{
		get => GetValue<SortEnumType?>("name");
    	set => SetValue("name", value);
	}

	[JsonPropertyName("street")]
	public SortEnumType? Street 
	{
		get => GetValue<SortEnumType?>("street");
    	set => SetValue("street", value);
	}

	[JsonPropertyName("postalCode")]
	public SortEnumType? PostalCode 
	{
		get => GetValue<SortEnumType?>("postalCode");
    	set => SetValue("postalCode", value);
	}

}