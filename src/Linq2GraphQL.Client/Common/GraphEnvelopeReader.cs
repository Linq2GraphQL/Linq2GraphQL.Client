using System.Buffers;
using System.Text.Json;

namespace Linq2GraphQL.Client.Common;

/// <summary>
///     Reads a GraphQL response envelope directly from UTF-8 bytes: only the requested
///     field under "data" is deserialized; sibling root fields are skipped without
///     materializing a typed value. No JsonDocument and no UTF-16 round trip.
/// </summary>
internal static class GraphEnvelopeReader
{
    internal static GraphResult<T> Read<T>(ReadOnlySpan<byte> utf8, string name, JsonSerializerOptions options)
    {
        if (utf8.Length == 0)
        {
            return new GraphResult<T> { Data = default };
        }

        var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);

        if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
        {
            List<GraphQueryError> errors = null;
            Dictionary<string, object> extensions = null;
            T data = default;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "data":
                        data = ReadData<T>(ref reader, name, options);
                        break;
                    case "errors":
                        errors = JsonSerializer.Deserialize<List<GraphQueryError>>(ref reader, options);
                        break;
                    case "extensions":
                        extensions =
                            JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new GraphResult<T> { Data = data, Errors = errors, Extensions = extensions };
        }

        throw new JsonException("GraphQL response envelope must be a JSON object.");
    }

    private static TData ReadData<TData>(ref Utf8JsonReader reader, string name, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("The 'data' property must be a JSON object or null.");
        }

        TData result = default;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            var fieldName = reader.GetString();
            reader.Read();

            if (fieldName == name)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    result = default;
                }
                else
                {
                    result = JsonSerializer.Deserialize<TData>(ref reader, options);
                }
            }
            else
            {
                // Multi-root query: never materialize fields we were not asked for.
                reader.Skip();
            }
        }

        return result;
    }
}
