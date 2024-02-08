using Linq2GraphQL.Client;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Linq2GraphQL.TestClientNullable;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum AddressType
{
    [EnumMember(Value = "DELIVERY")]
    Delivery,
    [EnumMember(Value = "INVOICE")]
    Invoice,
}