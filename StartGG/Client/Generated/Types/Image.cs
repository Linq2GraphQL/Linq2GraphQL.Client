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
/// An image
/// </summary>
public partial class Image : GraphQLTypeBase
{
    [GraphQLMember("id")]
    [JsonPropertyName("id")]
    public ID Id { get; set; }

    [GraphQLMember("height")]
    [JsonPropertyName("height")]
    public double? Height { get; set; }

    [GraphQLMember("ratio")]
    [JsonPropertyName("ratio")]
    public double? Ratio { get; set; }

    [GraphQLMember("type")]
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [GraphQLMember("url")]
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [GraphQLMember("width")]
    [JsonPropertyName("width")]
    public double? Width { get; set; }

}
