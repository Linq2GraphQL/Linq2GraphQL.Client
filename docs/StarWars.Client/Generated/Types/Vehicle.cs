using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace StarWars.Client;

public static class VehicleExtensions
{
    [GraphMethod("pilotConnection")]
    public static VehiclePilotsConnection PilotConnection(this Vehicle  vehicle, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return vehicle.GetMethodValue<VehiclePilotsConnection>("pilotConnection", after, first, before, last);
    }

    [GraphMethod("filmConnection")]
    public static VehicleFilmsConnection FilmConnection(this Vehicle  vehicle, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return vehicle.GetMethodValue<VehicleFilmsConnection>("filmConnection", after, first, before, last);
    }

}

public partial class Vehicle : GraphQLTypeBase, Node
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



    private LazyProperty<VehiclePilotsConnection> _pilotConnection = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
    public VehiclePilotsConnection PilotConnection => _pilotConnection.Value(() => GetFirstMethodValue<VehiclePilotsConnection>("pilotConnection"));
   // public VehiclePilotsConnection PilotConnection { get; set; }  



    private LazyProperty<VehicleFilmsConnection> _filmConnection = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
    public VehicleFilmsConnection FilmConnection => _filmConnection.Value(() => GetFirstMethodValue<VehicleFilmsConnection>("filmConnection"));
   // public VehicleFilmsConnection FilmConnection { get; set; }  


    [JsonPropertyName("created")]
	public string Created { get; set; }  


    [JsonPropertyName("edited")]
	public string Edited { get; set; }  


    [JsonPropertyName("id")]
	public string Id { get; set; }  






    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }
    }
