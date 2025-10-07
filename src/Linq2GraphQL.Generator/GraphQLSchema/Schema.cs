using System.Text.Json.Serialization;

namespace Linq2GraphQL.Generator;

public class Schema
{
    private static readonly List<string> BuiltInTypes = new()
    {
        //   "ID",
        "Int", "Float", "String", "Boolean"
    };

    public List<GraphqlType> Types { get; set; }

    [JsonPropertyName("queryType")] public GraphqlSchemaType SchemaQueryType { get; set; }

    [JsonPropertyName("mutationType")] public GraphqlSchemaType SchemaMutationType { get; set; }

    [JsonPropertyName("subscriptionType")] public GraphqlSchemaType SchemaSubscriptionType { get; set; }

    [JsonIgnore] public GraphqlType QueryType => Types?.FirstOrDefault(e => e.Name == SchemaQueryType?.Name);

    [JsonIgnore] public GraphqlType MutationType => Types?.FirstOrDefault(e => e.Name == SchemaMutationType?.Name);

    [JsonIgnore]
    public GraphqlType SubscriptionType => Types?.FirstOrDefault(e => e.Name == SchemaSubscriptionType?.Name);

    public List<GraphqlType> GetAllTypesExceptSystemTypes()
    {
        if (Types == null) return new();

        return Types.Where(e => e.Name != SchemaQueryType?.Name &&
                                !e.Name.StartsWith("__") &&
                                !BuiltInTypes.Contains(e.Name) &&
                                e.Name != SchemaMutationType?.Name &&
                                e.Name != SchemaSubscriptionType?.Name)
            .ToList();
    }

    public void PopulateFieldTypes()
    {
        if (Types == null) return;

        foreach (var typeGroup in Types.Where(e => e.AllFields != null && e.AllFields.Any())
                     .SelectMany(e => e.AllFields).GroupBy(e => e.Type.GetBaseBaseType().Name))
        {
            var graphQlType = GetGraphqlType(typeGroup.Key);

            foreach (var item in typeGroup)
            {
                item.GraphqlType = graphQlType;
            }
        }
    }


    public List<GraphqlType> GetClassTypes()
    {
        return GetAllTypesExceptSystemTypes()
            .Where(e => e.Kind == TypeKind.Object || e.Kind == TypeKind.InputObject || e.Kind == TypeKind.Union)
            .OrderBy(type => type.Name).ToList();
    }

    public List<GraphqlType> GetEnums()
    {
        return GetAllTypesExceptSystemTypes().Where(e => e.Kind == TypeKind.Enum).ToList();
    }

    public List<GraphqlType> GetCustomScalars()
    {
        var mappers = Helpers.TypeMapping;
        return GetAllTypesExceptSystemTypes().Where(e => e.Kind == TypeKind.Scalar && !mappers.ContainsKey(e.Name))
            .ToList();
    }

    public List<GraphqlType> GetInterfaces()
    {
        return GetAllTypesExceptSystemTypes().Where(e => e.Kind == TypeKind.Interface).ToList();
    }

    public GraphqlType GetGraphqlType(string name)
    {
        return Types?.FirstOrDefault(e => e.Name == name);
    }
}