using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Converters;

namespace StarWars.Client;

public static class NodeExtentions
{


    [GraphInterface]
    public static Film Film(this Node value)
    {
        if (value.__TypeName == "Film")
        {
            return (Film)value;
        }
        return null;
    }

    [GraphInterface]
    public static Person Person(this Node value)
    {
        if (value.__TypeName == "Person")
        {
            return (Person)value;
        }
        return null;
    }

    [GraphInterface]
    public static Planet Planet(this Node value)
    {
        if (value.__TypeName == "Planet")
        {
            return (Planet)value;
        }
        return null;
    }

    [GraphInterface]
    public static Species Species(this Node value)
    {
        if (value.__TypeName == "Species")
        {
            return (Species)value;
        }
        return null;
    }

    [GraphInterface]
    public static Starship Starship(this Node value)
    {
        if (value.__TypeName == "Starship")
        {
            return (Starship)value;
        }
        return null;
    }

    [GraphInterface]
    public static Vehicle Vehicle(this Node value)
    {
        if (value.__TypeName == "Vehicle")
        {
            return (Vehicle)value;
        }
        return null;
    }
}


internal class NodeConverter : InterfaceJsonConverter<Node>
{
    public override Node Deserialize(string typeName, JsonObject json) => typeName switch
    {
          "Film" => json.Deserialize<Film>(),
      "Person" => json.Deserialize<Person>(),
      "Planet" => json.Deserialize<Planet>(),
      "Species" => json.Deserialize<Species>(),
      "Starship" => json.Deserialize<Starship>(),
      "Vehicle" => json.Deserialize<Vehicle>(),
        _ => json.Deserialize< Node__Concrete>()
    };
}




[JsonConverter(typeof(NodeConverter))]
public interface Node 
{
	[JsonPropertyName("id")]
	public string Id { get; set; }  
    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }

}

internal class Node__Concrete : Node
{
	[JsonPropertyName("id")]
	public string Id { get; set; }  

    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }

}