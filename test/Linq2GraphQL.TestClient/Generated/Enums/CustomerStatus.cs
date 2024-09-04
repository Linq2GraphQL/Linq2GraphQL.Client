using Linq2GraphQL.Client;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum CustomerStatus
{
    [EnumMember(Value = "ACTIVE")]
    Active,
    [EnumMember(Value = "DISABLED")]
    Disabled,
}