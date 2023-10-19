using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.StarWars;

public partial class PersonVehiclesEdge 
{
	[JsonPropertyName("node")]
	public Vehicle Node { get; set; }  

	[JsonPropertyName("cursor")]
	public string Cursor { get; set; }  


}
