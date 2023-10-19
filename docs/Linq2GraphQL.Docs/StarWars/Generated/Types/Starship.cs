using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.StarWars;

public static class StarshipExtensions
{
    [GraphMethod("pilotConnection")]
    public static StarshipPilotsConnection PilotConnection(this Starship  starship, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return starship?.PilotConnection;
    }

    [GraphMethod("filmConnection")]
    public static StarshipFilmsConnection FilmConnection(this Starship  starship, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return starship?.FilmConnection;
    }

}

public partial class Starship : Node
{
	[JsonPropertyName("name")]
	public string Name { get; set; }  

	[JsonPropertyName("model")]
	public string Model { get; set; }  

	[JsonPropertyName("starshipClass")]
	public string StarshipClass { get; set; }  

	[JsonPropertyName("manufacturers")]
	public List<string> Manufacturers { get; set; }  

	[JsonPropertyName("costInCredits")]
	public float? CostInCredits { get; set; }  

	[JsonPropertyName("length")]
	public float? Length { get; set; }  

	[JsonPropertyName("crew")]
	public string Crew { get; set; }  

	[JsonPropertyName("passengers")]
	public string Passengers { get; set; }  

	[JsonPropertyName("maxAtmospheringSpeed")]
	public int? MaxAtmospheringSpeed { get; set; }  

	[JsonPropertyName("hyperdriveRating")]
	public float? HyperdriveRating { get; set; }  

	[JsonPropertyName("MGLT")]
	public int? MGLT { get; set; }  

	[JsonPropertyName("cargoCapacity")]
	public float? CargoCapacity { get; set; }  

	[JsonPropertyName("consumables")]
	public string Consumables { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphQLShadowProperty]
	[JsonPropertyName("pilotConnection")]
	public StarshipPilotsConnection PilotConnection { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphQLShadowProperty]
	[JsonPropertyName("filmConnection")]
	public StarshipFilmsConnection FilmConnection { get; set; }  

	[JsonPropertyName("created")]
	public string Created { get; set; }  

	[JsonPropertyName("edited")]
	public string Edited { get; set; }  

	[JsonPropertyName("id")]
	public string Id { get; set; }  


    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }
    }
