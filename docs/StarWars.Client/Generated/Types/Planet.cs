using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace StarWars.Client;

public static class PlanetExtensions
{
    [GraphMethod("residentConnection")]
    public static PlanetResidentsConnection ResidentConnection(this Planet  planet, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return planet?.ResidentConnection;
    }

    [GraphMethod("filmConnection")]
    public static PlanetFilmsConnection FilmConnection(this Planet  planet, [GraphArgument("String")] string after = null, [GraphArgument("Int")] int? first = null, [GraphArgument("String")] string before = null, [GraphArgument("Int")] int? last = null)
    {
	    return planet?.FilmConnection;
    }

}

public partial class Planet : Node
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

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphQLShadowProperty]
	[JsonPropertyName("residentConnection")]
	public PlanetResidentsConnection ResidentConnection { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphQLShadowProperty]
	[JsonPropertyName("filmConnection")]
	public PlanetFilmsConnection FilmConnection { get; set; }  

	[JsonPropertyName("created")]
	public string Created { get; set; }  

	[JsonPropertyName("edited")]
	public string Edited { get; set; }  

	[JsonPropertyName("id")]
	public string Id { get; set; }  


    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }
    }
