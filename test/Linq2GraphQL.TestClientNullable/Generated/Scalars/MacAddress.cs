
using Linq2GraphQL.Client;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;

namespace Linq2GraphQL.TestClientNullable;

/// <summary>
/// The `MacAddress` scalar type represents a IEEE 802 48-bit or 64-bit Mac address, represented as UTF-8 character sequences. The scalar follows the specifications defined in RFC7042 and RFC7043 respectively.
/// </summary>
[JsonConverter(typeof(CustomScalarConverter<MacAddress>))]
public partial class MacAddress : CustomScalar
{
   

}