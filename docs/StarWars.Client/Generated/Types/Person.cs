using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace StarWars.Client;

public static class PersonExtensions
{
    [GraphMethod("filmConnection")]
    public static PersonFilmsConnection FilmConnection(this Person  person, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return person.GetMethodValue<PersonFilmsConnection>("filmConnection", after, first, before, last);
    }

    [GraphMethod("starshipConnection")]
    public static PersonStarshipsConnection StarshipConnection(this Person  person, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return person.GetMethodValue<PersonStarshipsConnection>("starshipConnection", after, first, before, last);
    }

    [GraphMethod("vehicleConnection")]
    public static PersonVehiclesConnection VehicleConnection(this Person  person, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return person.GetMethodValue<PersonVehiclesConnection>("vehicleConnection", after, first, before, last);
    }

}

public partial class Person : GraphQLTypeBase, Node
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



    private LazyProperty<PersonFilmsConnection> _filmConnection = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
    public PersonFilmsConnection FilmConnection => _filmConnection.Value(() => GetFirstMethodValue<PersonFilmsConnection>("filmConnection"));
   // public PersonFilmsConnection FilmConnection { get; set; }  


    [JsonPropertyName("species")]
	public Species Species { get; set; }  



    private LazyProperty<PersonStarshipsConnection> _starshipConnection = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
    public PersonStarshipsConnection StarshipConnection => _starshipConnection.Value(() => GetFirstMethodValue<PersonStarshipsConnection>("starshipConnection"));
   // public PersonStarshipsConnection StarshipConnection { get; set; }  



    private LazyProperty<PersonVehiclesConnection> _vehicleConnection = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
    public PersonVehiclesConnection VehicleConnection => _vehicleConnection.Value(() => GetFirstMethodValue<PersonVehiclesConnection>("vehicleConnection"));
   // public PersonVehiclesConnection VehicleConnection { get; set; }  


    [JsonPropertyName("created")]
	public string Created { get; set; }  


    [JsonPropertyName("edited")]
	public string Edited { get; set; }  


    [JsonPropertyName("id")]
	public string Id { get; set; }  






    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }
    }
