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


public partial class TournamentLinks : GraphQLTypeBase
{
    [GraphQLMember("facebook")]
    [JsonPropertyName("facebook")]
    public string Facebook { get; set; }

    [GraphQLMember("discord")]
    [JsonPropertyName("discord")]
    public string Discord { get; set; }

}
