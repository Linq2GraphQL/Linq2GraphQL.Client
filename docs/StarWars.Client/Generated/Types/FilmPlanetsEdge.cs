using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace StarWars.Client;

public partial class FilmPlanetsEdge : GraphQLTypeBase
{
    [JsonPropertyName("node")]
	public Planet Node { get; set; }  


    [JsonPropertyName("cursor")]
	public string Cursor { get; set; }  






}
