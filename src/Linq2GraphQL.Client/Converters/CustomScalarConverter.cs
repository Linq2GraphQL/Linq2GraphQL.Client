using System.Text.Json;
using System.Text.Json.Serialization;

namespace Linq2GraphQL.Client;

public class CustomScalarConverter<TScalar> : JsonConverter<TScalar>
    where TScalar : CustomScalar, new()
{
    public override TScalar Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        object value = null;
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
            case JsonTokenType.None:
                return null;
            case JsonTokenType.String:
                value = reader.GetString();
                break;
            case JsonTokenType.Number:
                value = reader.GetDecimal();
                break;
            case JsonTokenType.True:
                value = true;
                break;
            case JsonTokenType.False:
                value = false;
                break;
            default:
                value = JsonDocument.ParseValue(ref reader);
                break;
        }

        return new() { Value = value };
    }

    public override void Write(Utf8JsonWriter writer, TScalar value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.InternalValue?.ToString());
    }
}