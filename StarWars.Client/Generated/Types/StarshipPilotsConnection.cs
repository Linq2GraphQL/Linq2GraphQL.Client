using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace StarWars.Client;

public partial class StarshipPilotsConnection : Linq2GraphQL.Client.Common.ICursorPaging
{
	[JsonPropertyName("pageInfo")]
	public Linq2GraphQL.Client.Common.PageInfo PageInfo { get; set; }  

	[JsonPropertyName("edges")]
	public List<StarshipPilotsEdge> Edges { get; set; }  

	[JsonPropertyName("totalCount")]
	public int? TotalCount { get; set; }  

	[JsonPropertyName("pilots")]
	public List<Person> Pilots { get; set; }  


}
