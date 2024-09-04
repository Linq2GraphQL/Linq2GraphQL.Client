using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClientNullable;

#pragma warning disable CS8618

public partial class Address : GraphQLTypeBase
{
    [JsonPropertyName("name")]
	public string Name { get; set; }  

    [JsonPropertyName("street")]
	public string Street { get; set; }  

    [JsonPropertyName("postalCode")]
	public string PostalCode { get; set; }  

}
