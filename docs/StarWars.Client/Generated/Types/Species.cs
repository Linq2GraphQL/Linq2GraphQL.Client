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


public static class SpeciesExtensions
{
    [GraphQLMember("personConnection")]
    public static SpeciesPeopleConnection PersonConnection(this Species  species, [GraphQLArgument("after", "String")] string after = null, [GraphQLArgument("first", "Int")] int? first = null, [GraphQLArgument("before", "String")] string before = null, [GraphQLArgument("last", "Int")] int? last = null)
    {
        return species.GetMethodValue<SpeciesPeopleConnection>("personConnection", after, first, before, last);
    }

    [GraphQLMember("filmConnection")]
    public static SpeciesFilmsConnection FilmConnection(this Species  species, [GraphQLArgument("after", "String")] string after = null, [GraphQLArgument("first", "Int")] int? first = null, [GraphQLArgument("before", "String")] string before = null, [GraphQLArgument("last", "Int")] int? last = null)
    {
        return species.GetMethodValue<SpeciesFilmsConnection>("filmConnection", after, first, before, last);
    }

}

/// <summary>
/// A type of person or character within the Star Wars Universe.
/// </summary>
public partial class Species : GraphQLTypeBase, Node
{
    /// <summary>
    /// The name of this species.
    /// </summary>
    [GraphQLMember("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The classification of this species, such as "mammal" or "reptile".
    /// </summary>
    [GraphQLMember("classification")]
    [JsonPropertyName("classification")]
    public string Classification { get; set; }

    /// <summary>
    /// The designation of this species, such as "sentient".
    /// </summary>
    [GraphQLMember("designation")]
    [JsonPropertyName("designation")]
    public string Designation { get; set; }

    /// <summary>
    /// The average height of this species in centimeters.
    /// </summary>
    [GraphQLMember("averageHeight")]
    [JsonPropertyName("averageHeight")]
    public double? AverageHeight { get; set; }

    /// <summary>
    /// The average lifespan of this species in years, null if unknown.
    /// </summary>
    [GraphQLMember("averageLifespan")]
    [JsonPropertyName("averageLifespan")]
    public int? AverageLifespan { get; set; }

    /// <summary>
    /// Common eye colors for this species, null if this species does not typically
/// have eyes.
    /// </summary>
    [GraphQLMember("eyeColors")]
    [JsonPropertyName("eyeColors")]
    public List<string> EyeColors { get; set; }

    /// <summary>
    /// Common hair colors for this species, null if this species does not typically
/// have hair.
    /// </summary>
    [GraphQLMember("hairColors")]
    [JsonPropertyName("hairColors")]
    public List<string> HairColors { get; set; }

    /// <summary>
    /// Common skin colors for this species, null if this species does not typically
/// have skin.
    /// </summary>
    [GraphQLMember("skinColors")]
    [JsonPropertyName("skinColors")]
    public List<string> SkinColors { get; set; }

    /// <summary>
    /// The language commonly spoken by this species.
    /// </summary>
    [GraphQLMember("language")]
    [JsonPropertyName("language")]
    public string Language { get; set; }

    /// <summary>
    /// A planet that this species originates from.
    /// </summary>
    [GraphQLMember("homeworld")]
    [JsonPropertyName("homeworld")]
    public Planet Homeworld { get; set; }

    private LazyProperty<SpeciesPeopleConnection> _personConnection = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public SpeciesPeopleConnection PersonConnection => _personConnection.Value(() => GetFirstMethodValue<SpeciesPeopleConnection>("personConnection"));

    private LazyProperty<SpeciesFilmsConnection> _filmConnection = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public SpeciesFilmsConnection FilmConnection => _filmConnection.Value(() => GetFirstMethodValue<SpeciesFilmsConnection>("filmConnection"));

    /// <summary>
    /// The ISO 8601 date format of the time that this resource was created.
    /// </summary>
    [GraphQLMember("created")]
    [JsonPropertyName("created")]
    public string Created { get; set; }

    /// <summary>
    /// The ISO 8601 date format of the time that this resource was edited.
    /// </summary>
    [GraphQLMember("edited")]
    [JsonPropertyName("edited")]
    public string Edited { get; set; }

    /// <summary>
    /// The ID of an object
    /// </summary>
    [GraphQLMember("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [GraphQLMember("__typename")]
    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }
}
