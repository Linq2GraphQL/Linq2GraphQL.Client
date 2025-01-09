using System.Text.Json.Serialization;
using System.Text.Json;
using Linq2GraphQL.Client;


namespace Linq2GraphQL.Client
{
  
    public class CustomScalarConverter<TScalar> : JsonConverter<TScalar>
        where TScalar : CustomScalar, new()
    {
        public override TScalar Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (value is null)
            {
                return null;
            }

            var scalar = new TScalar
            {
                InternalValue = value
            };

            return scalar;
        }

        public override void Write(Utf8JsonWriter writer, TScalar value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.InternalValue);
        }

    }
}
