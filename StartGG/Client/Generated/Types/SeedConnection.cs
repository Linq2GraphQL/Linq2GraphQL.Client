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

namespace StartGG.Client;


public partial class SeedConnection : GraphQLTypeBase, Linq2GraphQL.Client.Common.ICursorPaging
{
    [GraphQLMember("pageInfo")]
    [JsonPropertyName("pageInfo")]
    public Linq2GraphQL.Client.Common.PageInfo PageInfo { get; set; }

    [GraphQLMember("nodes")]
    [JsonPropertyName("nodes")]
    public List<Seed> Nodes { get; set; }

}
