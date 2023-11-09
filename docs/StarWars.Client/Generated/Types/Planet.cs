using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace StarWars.Client;

public static class PlanetExtensions
{
    [GraphMethod("residentConnection")]
    public static PlanetResidentsConnection ResidentConnection(this Planet  planet, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return planet.GetMethodValue<PlanetResidentsConnection>("residentConnection", after, first, before, last);
    }

    [GraphMethod("filmConnection")]
    public static PlanetFilmsConnection FilmConnection(this Planet  planet, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return planet.GetMethodValue<PlanetFilmsConnection>("filmConnection", after, first, before, last);
    }

}

public partial class Planet : GraphQLTypeBase, Node
{
    [JsonPropertyName("name")]
	public string Name { get; set; }  


    [JsonPropertyName("diameter")]
	public int? Diameter { get; set; }  


    [JsonPropertyName("rotationPeriod")]
	public int? RotationPeriod { get; set; }  


    [JsonPropertyName("orbitalPeriod")]
	public int? OrbitalPeriod { get; set; }  


    [JsonPropertyName("gravity")]
	public string Gravity { get; set; }  


    [JsonPropertyName("population")]
	public float? Population { get; set; }  


    [JsonPropertyName("climates")]
	public List<string> Climates { get; set; }  


    [JsonPropertyName("terrains")]
	public List<string> Terrains { get; set; }  


    [JsonPropertyName("surfaceWater")]
	public float? SurfaceWater { get; set; }  



    private LazyProperty<PlanetResidentsConnection> _residentConnection = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
    public PlanetResidentsConnection ResidentConnection => _residentConnection.Value(() => GetFirstMethodValue<PlanetResidentsConnection>("residentConnection"));
   // public PlanetResidentsConnection ResidentConnection { get; set; }  



    private LazyProperty<PlanetFilmsConnection> _filmConnection = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
    public PlanetFilmsConnection FilmConnection => _filmConnection.Value(() => GetFirstMethodValue<PlanetFilmsConnection>("filmConnection"));
   // public PlanetFilmsConnection FilmConnection { get; set; }  


    [JsonPropertyName("created")]
	public string Created { get; set; }  


    [JsonPropertyName("edited")]
	public string Edited { get; set; }  


    [JsonPropertyName("id")]
	public string Id { get; set; }  






    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }
    }
