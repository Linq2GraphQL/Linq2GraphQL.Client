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
/// An OAuth ProfileAuthorization object
/// </summary>
public partial class ProfileAuthorization : GraphQLTypeBase
{
    [GraphQLMember("id")]
    [JsonPropertyName("id")]
    public ID Id { get; set; }

    /// <summary>
    /// The id given by the external service
    /// </summary>
    [GraphQLMember("externalId")]
    [JsonPropertyName("externalId")]
    public string ExternalId { get; set; }

    /// <summary>
    /// The username given by the external service (including discriminator if discord)
    /// </summary>
    [GraphQLMember("externalUsername")]
    [JsonPropertyName("externalUsername")]
    public string ExternalUsername { get; set; }

    [GraphQLMember("stream")]
    [JsonPropertyName("stream")]
    public Stream Stream { get; set; }

    /// <summary>
    /// The name of the external service providing this auth i.e. "twitch"
    /// </summary>
    [GraphQLMember("type")]
    [JsonPropertyName("type")]
    public AuthorizationType? Type { get; set; }

    [GraphQLMember("url")]
    [JsonPropertyName("url")]
    public string Url { get; set; }

}
