//---------------------------------------------------------------------
// This code was automatically generated by Linq2GraphQL
// Please don't edit this file
// Github:https://github.com/linq2graphql/linq2graphql.client
// Url: https://linq2graphql.com
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace StarWars.Client;


/// <summary>
/// A connection to a list of items.
/// </summary>
public partial class PlanetsConnection : GraphQLTypeBase, Linq2GraphQL.Client.Common.ICursorPaging
{
    /// <summary>
    /// Information to aid in pagination.
    /// </summary>
    [GraphQLMember("pageInfo")]
    [JsonPropertyName("pageInfo")]
    public Linq2GraphQL.Client.Common.PageInfo PageInfo { get; set; }

    /// <summary>
    /// A list of edges.
    /// </summary>
    [GraphQLMember("edges")]
    [JsonPropertyName("edges")]
    public List<PlanetsEdge> Edges { get; set; }

    /// <summary>
    /// A count of the total number of objects in this connection, ignoring pagination.
/// This allows a client to fetch the first five objects by passing "5" as the
/// argument to "first", then fetch the total count so it could display "5 of 83",
/// for example.
    /// </summary>
    [GraphQLMember("totalCount")]
    [JsonPropertyName("totalCount")]
    public int? TotalCount { get; set; }

    /// <summary>
    /// A list of all of the objects returned in the connection. This is a convenience
/// field provided for quickly exploring the API; rather than querying for
/// "{ edges { node } }" when no edge data is needed, this field can be be used
/// instead. Note that when clients like Relay need to fetch the "cursor" field on
/// the edge to enable efficient pagination, this shortcut cannot be used, and the
/// full "{ edges { node } }" version should be used instead.
    /// </summary>
    [GraphQLMember("planets")]
    [JsonPropertyName("planets")]
    public List<Planet> Planets { get; set; }

}
