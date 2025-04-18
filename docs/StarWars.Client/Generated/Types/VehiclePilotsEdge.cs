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
/// An edge in a connection.
/// </summary>
public partial class VehiclePilotsEdge : GraphQLTypeBase
{
    /// <summary>
    /// The item at the end of the edge
    /// </summary>
    [GraphQLMember("node")]
    [JsonPropertyName("node")]
    public Person Node { get; set; }

    /// <summary>
    /// A cursor for use in pagination
    /// </summary>
    [GraphQLMember("cursor")]
    [JsonPropertyName("cursor")]
    public string Cursor { get; set; }

}
