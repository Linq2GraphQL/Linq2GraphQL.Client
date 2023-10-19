using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Converters;

namespace Linq2GraphQL.StarWars;

[JsonConverter(typeof(InterfaceConverter<Node__Concrete,  Node>))]
public interface Node 
{
	[JsonPropertyName("id")]
	public string Id { get; set; }  
    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }

}

internal class Node__Concrete : Node
{
	[JsonPropertyName("id")]
	public string Id { get; set; }  

    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }

}