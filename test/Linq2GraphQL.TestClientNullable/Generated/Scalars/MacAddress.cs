
//---------------------------------------------------------------------
// This code was automatically generated by Linq2GraphQL
// Please don't edit this file
// Github:https://github.com/linq2graphql/linq2graphql.client
// Url: https://linq2graphql.com
// Generation Date: den 8 september 2024 17:02:30
//---------------------------------------------------------------------


using Linq2GraphQL.Client;
using System.Text.Json.Serialization;

namespace Linq2GraphQL.TestClientNullable;

    /// <summary>
    /// The `MacAddress` scalar type represents a IEEE 802 48-bit or 64-bit Mac address, represented as UTF-8 character sequences. The scalar follows the specifications defined in RFC7042 and RFC7043 respectively.
    /// </summary>
    [JsonConverter(typeof(CustomScalarConverter<MacAddress>))]
    public partial class MacAddress : CustomScalar {}