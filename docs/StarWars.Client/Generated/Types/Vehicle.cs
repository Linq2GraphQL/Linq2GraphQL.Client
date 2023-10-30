using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace StarWars.Client;

public static class VehicleExtensions
{
    [GraphMethod("pilotConnection")]
    public static VehiclePilotsConnection PilotConnection(this Vehicle  vehicle, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return vehicle?.PilotConnection;
    }

    [GraphMethod("filmConnection")]
    public static VehicleFilmsConnection FilmConnection(this Vehicle  vehicle, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return vehicle?.FilmConnection;
    }

}

public partial class Vehicle : Node
{
	[JsonPropertyName("name")]
	public string Name { get; set; }  

	[JsonPropertyName("model")]
	public string Model { get; set; }  

	[JsonPropertyName("vehicleClass")]
	public string VehicleClass { get; set; }  

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

	[JsonPropertyName("cargoCapacity")]
	public float? CargoCapacity { get; set; }  

	[JsonPropertyName("consumables")]
	public string Consumables { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
	[JsonPropertyName("pilotConnection")]
	public VehiclePilotsConnection PilotConnection { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
	[JsonPropertyName("filmConnection")]
	public VehicleFilmsConnection FilmConnection { get; set; }  

	[JsonPropertyName("created")]
	public string Created { get; set; }  

	[JsonPropertyName("edited")]
	public string Edited { get; set; }  

	[JsonPropertyName("id")]
	public string Id { get; set; }  


    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }
    }
