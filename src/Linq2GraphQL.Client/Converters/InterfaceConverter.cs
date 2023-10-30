using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Linq2GraphQL.Client.Converters
{
    public class InterfaceConverter<TImplementation, TInterface> : JsonConverter<TInterface> where TImplementation : class, TInterface
    {
        public override TInterface Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonSerializer.Deserialize<TImplementation>(ref reader, options);

        public override void Write(Utf8JsonWriter writer, TInterface value, JsonSerializerOptions options)
        {
        }
    }

    public abstract class InterfaceJsonConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var json = JsonSerializer.Deserialize<JsonObject>(ref reader);
            if (json is null)
            {
                return default;
            }

            if (!json.ContainsKey("__typename"))
            {
                return default;
            }

            var type = json["__typename"]!.ToString();
            var value = Deserialize(type, json);
            return value;
        }

        public abstract T Deserialize(string typeName, JsonObject json);

        public override void Write(
            Utf8JsonWriter writer,
            T value,
            JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    JsonSerializer.Serialize(writer, default!, options);
                    break;
                default:
                    {
                        JsonSerializer.Serialize(writer, value, value.GetType(), options);
                        break;
                    }
            }
        }
    }


}











