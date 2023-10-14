using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Converters;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(InterfaceConverter<IAnimal__Concrete,  IAnimal>))]
public interface IAnimal 
{
	[JsonPropertyName("name")]
	public string Name { get; set; }  
	[JsonPropertyName("numberOfLegs")]
	public int NumberOfLegs { get; set; }  
    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }

}

internal class IAnimal__Concrete : IAnimal
{
	[JsonPropertyName("name")]
	public string Name { get; set; }  
	[JsonPropertyName("numberOfLegs")]
	public int NumberOfLegs { get; set; }  

    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }

}