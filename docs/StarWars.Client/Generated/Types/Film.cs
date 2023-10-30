using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace StarWars.Client;

public static class FilmExtensions
{
    [GraphMethod("speciesConnection")]
    public static FilmSpeciesConnection SpeciesConnection(this Film  film, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return film?.SpeciesConnection;
    }

    [GraphMethod("starshipConnection")]
    public static FilmStarshipsConnection StarshipConnection(this Film  film, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return film?.StarshipConnection;
    }

    [GraphMethod("vehicleConnection")]
    public static FilmVehiclesConnection VehicleConnection(this Film  film, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return film?.VehicleConnection;
    }

    [GraphMethod("characterConnection")]
    public static FilmCharactersConnection CharacterConnection(this Film  film, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return film?.CharacterConnection;
    }

    [GraphMethod("planetConnection")]
    public static FilmPlanetsConnection PlanetConnection(this Film  film, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return film?.PlanetConnection;
    }

}

public partial class Film : Node
{
	[JsonPropertyName("title")]
	public string Title { get; set; }  

	[JsonPropertyName("episodeID")]
	public int? EpisodeID { get; set; }  

	[JsonPropertyName("openingCrawl")]
	public string OpeningCrawl { get; set; }  

	[JsonPropertyName("director")]
	public string Director { get; set; }  

	[JsonPropertyName("producers")]
	public List<string> Producers { get; set; }  

	[JsonPropertyName("releaseDate")]
	public string ReleaseDate { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
	[JsonPropertyName("speciesConnection")]
	public FilmSpeciesConnection SpeciesConnection { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
	[JsonPropertyName("starshipConnection")]
	public FilmStarshipsConnection StarshipConnection { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
	[JsonPropertyName("vehicleConnection")]
	public FilmVehiclesConnection VehicleConnection { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
	[JsonPropertyName("characterConnection")]
	public FilmCharactersConnection CharacterConnection { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
	[JsonPropertyName("planetConnection")]
	public FilmPlanetsConnection PlanetConnection { get; set; }  

	[JsonPropertyName("created")]
	public string Created { get; set; }  

	[JsonPropertyName("edited")]
	public string Edited { get; set; }  

	[JsonPropertyName("id")]
	public string Id { get; set; }  


    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }
    }
