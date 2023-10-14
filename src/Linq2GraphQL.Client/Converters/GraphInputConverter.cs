using System.Text.Json;
using System.Text.Json.Serialization;

namespace Linq2GraphQL.Client;

public class GraphInputConverter<ToGraphInputBase> : JsonConverter<ToGraphInputBase>
    where ToGraphInputBase : GraphInputBase
{
    public override ToGraphInputBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, ToGraphInputBase value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.GetValues(), options);
    }
}