using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace StarWars.Client;

public static class PersonExtensions
{
    [GraphMethod("filmConnection")]
    public static PersonFilmsConnection FilmConnection(this Person  person, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return person?.FilmConnection;
    }

    [GraphMethod("starshipConnection")]
    public static PersonStarshipsConnection StarshipConnection(this Person  person, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return person?.StarshipConnection;
    }

    [GraphMethod("vehicleConnection")]
    public static PersonVehiclesConnection VehicleConnection(this Person  person, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return person?.VehicleConnection;
    }

}

public partial class Person : Node
{
	[JsonPropertyName("name")]
	public string Name { get; set; }  

	[JsonPropertyName("birthYear")]
	public string BirthYear { get; set; }  

	[JsonPropertyName("eyeColor")]
	public string EyeColor { get; set; }  

	[JsonPropertyName("gender")]
	public string Gender { get; set; }  

	[JsonPropertyName("hairColor")]
	public string HairColor { get; set; }  

	[JsonPropertyName("height")]
	public int? Height { get; set; }  

	[JsonPropertyName("mass")]
	public float? Mass { get; set; }  

	[JsonPropertyName("skinColor")]
	public string SkinColor { get; set; }  

	[JsonPropertyName("homeworld")]
	public Planet Homeworld { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphQLShadowProperty]
	[JsonPropertyName("filmConnection")]
	public PersonFilmsConnection FilmConnection { get; set; }  

	[JsonPropertyName("species")]
	public Species Species { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphQLShadowProperty]
	[JsonPropertyName("starshipConnection")]
	public PersonStarshipsConnection StarshipConnection { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphQLShadowProperty]
	[JsonPropertyName("vehicleConnection")]
	public PersonVehiclesConnection VehicleConnection { get; set; }  

	[JsonPropertyName("created")]
	public string Created { get; set; }  

	[JsonPropertyName("edited")]
	public string Edited { get; set; }  

	[JsonPropertyName("id")]
	public string Id { get; set; }  


    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }
    }
