using Linq2GraphQL.Client;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum SortEnumType
{
    [EnumMember(Value = "ASC")]
    Asc,
    [EnumMember(Value = "DESC")]
    Desc,
    /// <summary>
    /// Unknown values are mapped to this member. 
    /// Generated via --es/-enum-strategy command line option upon generation. 
    /// Don't set explicitly. 
    /// </summary>
    [EnumMember(Value = "")]
    __Unknown
}