using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace StarWars.Client;

public partial class FilmPlanetsEdge 
{
	[JsonPropertyName("node")]
	public Planet Node { get; set; }  

	[JsonPropertyName("cursor")]
	public string Cursor { get; set; }  


}
