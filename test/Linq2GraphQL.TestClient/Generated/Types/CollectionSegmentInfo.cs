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

namespace Linq2GraphQL.TestClient;


/// <summary>
/// Information about the offset pagination.
/// </summary>
public partial class CollectionSegmentInfo : GraphQLTypeBase
{
    /// <summary>
    /// Indicates whether more items exist following the set defined by the clients arguments.
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    [GraphQLMember("hasNextPage")]
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Indicates whether more items exist prior the set defined by the clients arguments.
    /// </summary>
    [JsonPropertyName("hasPreviousPage")]
    [GraphQLMember("hasPreviousPage")]
    public bool HasPreviousPage { get; set; }

}
