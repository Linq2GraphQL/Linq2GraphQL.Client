using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace StarWars.Client;

public partial class FilmVehiclesEdge 
{
	[JsonPropertyName("node")]
	public Vehicle Node { get; set; }  

	[JsonPropertyName("cursor")]
	public string Cursor { get; set; }  


}
