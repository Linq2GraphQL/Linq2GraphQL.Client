using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.StarWars;

public partial class FilmSpeciesConnection : Linq2GraphQL.Client.Common.ICursorPaging
{
	[JsonPropertyName("pageInfo")]
	public Linq2GraphQL.Client.Common.PageInfo PageInfo { get; set; }  

	[JsonPropertyName("edges")]
	public List<FilmSpeciesEdge> Edges { get; set; }  

	[JsonPropertyName("totalCount")]
	public int? TotalCount { get; set; }  

	[JsonPropertyName("species")]
	public List<Species> Species { get; set; }  


}
