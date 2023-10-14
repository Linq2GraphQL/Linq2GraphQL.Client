using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Linq2GraphQL.Client.Converters
{
    public class InterfaceConverter<TImplementation, TInterface> : JsonConverter<TInterface>
    where TImplementation : class, TInterface
    {
        public override TInterface Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonSerializer.Deserialize<TImplementation>(ref reader, options);

        public override void Write(Utf8JsonWriter writer, TInterface value, JsonSerializerOptions options)
        {
        }
    }
}
