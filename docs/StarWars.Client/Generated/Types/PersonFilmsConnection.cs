using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace StarWars.Client;

public partial class PersonFilmsConnection : GraphQLTypeBase, Linq2GraphQL.Client.Common.ICursorPaging
{
    [JsonPropertyName("pageInfo")]
	public Linq2GraphQL.Client.Common.PageInfo PageInfo { get; set; }  


    [JsonPropertyName("edges")]
	public List<PersonFilmsEdge> Edges { get; set; }  


    [JsonPropertyName("totalCount")]
	public int? TotalCount { get; set; }  


    [JsonPropertyName("films")]
	public List<Film> Films { get; set; }  






}
