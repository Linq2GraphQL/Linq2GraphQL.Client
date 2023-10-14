using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Linq2GraphQL.Client.Schema;

public class GraphQLSchema
{
    [JsonPropertyName("types")] public List<GraphQLType> Types { get; set; }


    public bool TypePropertyExists(string typeName, string fieldName)
    {
        if (fieldName.ToLower() == "__typename" ) { return true; }

        var type = Types.FirstOrDefault(x => x.Name == typeName);
        if (type?.Fields.FirstOrDefault(x => x.Name == fieldName) == null)
        {
            return false;
        }

        return true;
    }
}

[DebuggerDisplay("{Name}")]
public class GraphQLType
{
    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("fields")] public List<GraphQLField> Fields { get; set; }
}

public class GraphQLField
{
    [JsonPropertyName("name")] public string Name { get; set; }
}