using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<AddressInput>))]
public partial class AddressInput : GraphInputBase
{
	[JsonPropertyName("name")]
	public string Name 
	{
		get => GetValue<string>("name");
    	set => SetValue("name", value);
	}

	[JsonPropertyName("street")]
	public string Street 
	{
		get => GetValue<string>("street");
    	set => SetValue("street", value);
	}

	[JsonPropertyName("postalCode")]
	public string PostalCode 
	{
		get => GetValue<string>("postalCode");
    	set => SetValue("postalCode", value);
	}

}