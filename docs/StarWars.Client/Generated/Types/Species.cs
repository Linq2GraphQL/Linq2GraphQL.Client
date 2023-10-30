using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace StarWars.Client;

public static class SpeciesExtensions
{
    [GraphMethod("personConnection")]
    public static SpeciesPeopleConnection PersonConnection(this Species  species, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return species?.PersonConnection;
    }

    [GraphMethod("filmConnection")]
    public static SpeciesFilmsConnection FilmConnection(this Species  species, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return species?.FilmConnection;
    }

}

public partial class Species : Node
{
	[JsonPropertyName("name")]
	public string Name { get; set; }  

	[JsonPropertyName("classification")]
	public string Classification { get; set; }  

	[JsonPropertyName("designation")]
	public string Designation { get; set; }  

	[JsonPropertyName("averageHeight")]
	public float? AverageHeight { get; set; }  

	[JsonPropertyName("averageLifespan")]
	public int? AverageLifespan { get; set; }  

	[JsonPropertyName("eyeColors")]
	public List<string> EyeColors { get; set; }  

	[JsonPropertyName("hairColors")]
	public List<string> HairColors { get; set; }  

	[JsonPropertyName("skinColors")]
	public List<string> SkinColors { get; set; }  

	[JsonPropertyName("language")]
	public string Language { get; set; }  

	[JsonPropertyName("homeworld")]
	public Planet Homeworld { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
	[JsonPropertyName("personConnection")]
	public SpeciesPeopleConnection PersonConnection { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
	[JsonPropertyName("filmConnection")]
	public SpeciesFilmsConnection FilmConnection { get; set; }  

	[JsonPropertyName("created")]
	public string Created { get; set; }  

	[JsonPropertyName("edited")]
	public string Edited { get; set; }  

	[JsonPropertyName("id")]
	public string Id { get; set; }  


    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }
    }
