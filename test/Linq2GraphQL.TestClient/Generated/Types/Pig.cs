using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClient;


public partial class Pig : GraphQLTypeBase, IAnimal
{
    [JsonPropertyName("name")]
	public string Name { get; set; }  

    [JsonPropertyName("numberOfLegs")]
	public int NumberOfLegs { get; set; }  

    [JsonPropertyName("speed")]
	public int Speed { get; set; }  

    [JsonPropertyName("spices")]
	public string Spices { get; set; }  

    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }
}
