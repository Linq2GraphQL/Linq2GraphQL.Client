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


public static class ShopLevelExtensions
{
    [GraphQLMember("images")]
    public static List<Image> Images(this ShopLevel  shopLevel, [GraphQLArgument("type", "String")] string type = null)
    {
        return shopLevel.GetMethodValue<List<Image>>("images", type);
    }

}

/// <summary>
/// A shop level
/// </summary>
public partial class ShopLevel : GraphQLTypeBase
{
    [GraphQLMember("id")]
    [JsonPropertyName("id")]
    public ID Id { get; set; }

    [GraphQLMember("currAmount")]
    [JsonPropertyName("currAmount")]
    public double? CurrAmount { get; set; }

    [GraphQLMember("description")]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [GraphQLMember("goalAmount")]
    [JsonPropertyName("goalAmount")]
    public double? GoalAmount { get; set; }

    private LazyProperty<List<Image>> _images = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public List<Image> Images => _images.Value(() => GetFirstMethodValue<List<Image>>("images"));

    [GraphQLMember("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }

}
