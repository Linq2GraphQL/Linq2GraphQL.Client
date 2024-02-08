using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClient;

public partial class AnimalsConnection : GraphQLTypeBase, Linq2GraphQL.Client.Common.ICursorPaging
{
    [JsonPropertyName("pageInfo")]
	public Linq2GraphQL.Client.Common.PageInfo PageInfo { get; set; }  

    [JsonPropertyName("edges")]
	public List<AnimalsEdge> Edges { get; set; }  

    [JsonPropertyName("nodes")]
	public List<IAnimal> Nodes { get; set; }  

    [JsonPropertyName("totalCount")]
	public int TotalCount { get; set; }  

}
