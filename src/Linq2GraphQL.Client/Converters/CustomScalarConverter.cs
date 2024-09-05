using System.Text.Json.Serialization;
using System.Text.Json;


namespace Linq2GraphQL.Client
{
    public class CustomScalar
    {
        public string Value { get; set; }

        public override string ToString() => Value;
    }

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
                Value = value
            };

            return scalar;
        }

        public override void Write(Utf8JsonWriter writer, TScalar value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }

    }
}
