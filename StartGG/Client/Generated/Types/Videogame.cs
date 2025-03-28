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


public static class VideogameExtensions
{
    [GraphQLMember("images")]
    public static List<Image> Images(this Videogame  videogame, [GraphQLArgument("type", "String")] string type = null)
    {
        return videogame.GetMethodValue<List<Image>>("images", type);
    }

}

/// <summary>
/// A videogame
/// </summary>
public partial class Videogame : GraphQLTypeBase
{
    [GraphQLMember("id")]
    [JsonPropertyName("id")]
    public ID Id { get; set; }

    /// <summary>
    /// All characters for this videogame
    /// </summary>
    [GraphQLMember("characters")]
    [JsonPropertyName("characters")]
    public List<Character> Characters { get; set; }

    [GraphQLMember("displayName")]
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    private LazyProperty<List<Image>> _images = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public List<Image> Images => _images.Value(() => GetFirstMethodValue<List<Image>>("images"));

    [GraphQLMember("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [GraphQLMember("slug")]
    [JsonPropertyName("slug")]
    public string Slug { get; set; }

    /// <summary>
    /// All stages for this videogame
    /// </summary>
    [GraphQLMember("stages")]
    [JsonPropertyName("stages")]
    public List<Stage> Stages { get; set; }

}
