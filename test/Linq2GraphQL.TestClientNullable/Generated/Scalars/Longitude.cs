
using Linq2GraphQL.Client;
using System.Text.Json.Serialization;

namespace Linq2GraphQL.TestClientNullable;

    /// <summary>
    /// The Longitude scalar type is a valid decimal degrees longitude number.
    /// </summary>
    [JsonConverter(typeof(CustomScalarConverter<Longitude>))]
    public partial class Longitude : CustomScalar {}