using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClientNullable;

#pragma warning disable CS8618

public partial class Person : GraphQLTypeBase
{
    [JsonPropertyName("name")]
	public string Name { get; set; }  

    [JsonPropertyName("macAddress")]
	public MacAddress? MacAddress { get; set; }  

    [JsonPropertyName("longitude")]
	public Longitude? Longitude { get; set; }  

}
