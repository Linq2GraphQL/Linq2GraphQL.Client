using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Converters;

namespace Linq2GraphQL.TestClient;

public static class IAnimalExtentions
{
    [GraphInterfaceProperty]
    public static Pig Pig(this IAnimal animal)
    {
        if (animal.__TypeName == "Pig")
        {
            return (Pig)animal;
        }
        return null;
    }

    [GraphInterfaceProperty]
    public static Spider Spider(this IAnimal animal)
    {
        if (animal.__TypeName == "Spider")
        {
            return (Spider)animal;
        }
        return null;
    }
}


internal class IAnimalConverter : InterfaceJsonConverter<IAnimal>
{
    public override IAnimal Deserialize(string typeName, JsonObject json) => typeName switch
    {
        "Pig" => json.Deserialize<Pig>(),
        "Spider" => json.Deserialize<Spider>(),
        _ => json.Deserialize<IAnimal__Concrete>()
    };
}


//[JsonConverter(typeof(InterfaceConverter<IAnimal__Concrete,  IAnimal>))]
//[JsonConverter(typeof(InterfaceTypeConverter<IAnimal>))]
[JsonConverter(typeof(IAnimalConverter))]
public interface IAnimal
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("numberOfLegs")]
    public int NumberOfLegs { get; set; }
    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }



}

public class IAnimal__Concrete : IAnimal
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("numberOfLegs")]
    public int NumberOfLegs { get; set; }

    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }



}