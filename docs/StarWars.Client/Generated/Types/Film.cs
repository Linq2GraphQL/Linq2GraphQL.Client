using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace StarWars.Client;

public static class FilmExtensions
{
    [GraphMethod("speciesConnection")]
    public static FilmSpeciesConnection SpeciesConnection(this Film  film, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return film.GetMethodValue<FilmSpeciesConnection>("speciesConnection", after, first, before, last);
    }

    [GraphMethod("starshipConnection")]
    public static FilmStarshipsConnection StarshipConnection(this Film  film, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return film.GetMethodValue<FilmStarshipsConnection>("starshipConnection", after, first, before, last);
    }

    [GraphMethod("vehicleConnection")]
    public static FilmVehiclesConnection VehicleConnection(this Film  film, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return film.GetMethodValue<FilmVehiclesConnection>("vehicleConnection", after, first, before, last);
    }

    [GraphMethod("characterConnection")]
    public static FilmCharactersConnection CharacterConnection(this Film  film, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return film.GetMethodValue<FilmCharactersConnection>("characterConnection", after, first, before, last);
    }

    [GraphMethod("planetConnection")]
    public static FilmPlanetsConnection PlanetConnection(this Film  film, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return film.GetMethodValue<FilmPlanetsConnection>("planetConnection", after, first, before, last);
    }

}

public partial class Film : GraphQLTypeBase, Node
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



    private LazyProperty<FilmSpeciesConnection> _speciesConnection = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
    public FilmSpeciesConnection SpeciesConnection => _speciesConnection.Value(() => GetFirstMethodValue<FilmSpeciesConnection>("speciesConnection"));
   // public FilmSpeciesConnection SpeciesConnection { get; set; }  



    private LazyProperty<FilmStarshipsConnection> _starshipConnection = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
    public FilmStarshipsConnection StarshipConnection => _starshipConnection.Value(() => GetFirstMethodValue<FilmStarshipsConnection>("starshipConnection"));
   // public FilmStarshipsConnection StarshipConnection { get; set; }  



    private LazyProperty<FilmVehiclesConnection> _vehicleConnection = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
    public FilmVehiclesConnection VehicleConnection => _vehicleConnection.Value(() => GetFirstMethodValue<FilmVehiclesConnection>("vehicleConnection"));
   // public FilmVehiclesConnection VehicleConnection { get; set; }  



    private LazyProperty<FilmCharactersConnection> _characterConnection = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
    public FilmCharactersConnection CharacterConnection => _characterConnection.Value(() => GetFirstMethodValue<FilmCharactersConnection>("characterConnection"));
   // public FilmCharactersConnection CharacterConnection { get; set; }  



    private LazyProperty<FilmPlanetsConnection> _planetConnection = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
    public FilmPlanetsConnection PlanetConnection => _planetConnection.Value(() => GetFirstMethodValue<FilmPlanetsConnection>("planetConnection"));
   // public FilmPlanetsConnection PlanetConnection { get; set; }  


    [JsonPropertyName("created")]
	public string Created { get; set; }  


    [JsonPropertyName("edited")]
	public string Edited { get; set; }  


    [JsonPropertyName("id")]
	public string Id { get; set; }  






    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }
    }
