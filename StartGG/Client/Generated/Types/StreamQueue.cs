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


/// <summary>
/// A Stream queue object
/// </summary>
public partial class StreamQueue : GraphQLTypeBase
{
    [GraphQLMember("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The sets on the stream
    /// </summary>
    [GraphQLMember("sets")]
    [JsonPropertyName("sets")]
    public List<Set> Sets { get; set; }

    /// <summary>
    /// The stream on the queue
    /// </summary>
    [GraphQLMember("stream")]
    [JsonPropertyName("stream")]
    public Streams Stream { get; set; }

}
