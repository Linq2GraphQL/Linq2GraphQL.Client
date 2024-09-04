using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClientNullable;

public partial class Address : GraphQLTypeBase
{
    [JsonPropertyName("name")]
	public required string Name { get; set; }  

    [JsonPropertyName("street")]
	public required string Street { get; set; }  

    [JsonPropertyName("postalCode")]
	public required string PostalCode { get; set; }  

}
