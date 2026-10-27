using System.Text.Json.Serialization;

namespace Linq2GraphQL.Generator;

public class Schema
{
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
                                !Helpers.BuiltInScalars.Contains(e.Name) &&
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

    /// <summary>
    ///     Every scalar in the schema with the C# type it maps to by default, before any scalarMappings
    ///     override, sorted by name.
    /// </summary>
    public List<ScalarInfo> GetScalars()
    {
        if (Types == null) return new();

        return Types.Where(e => e.Kind == TypeKind.Scalar && !e.Name.StartsWith("__"))
            .OrderBy(e => e.Name, StringComparer.Ordinal)
            .Select(e => new ScalarInfo
            {
                Name = e.Name,
                Description = e.Description,
                IsBuiltIn = Helpers.BuiltInScalars.Contains(e.Name),
                DefaultTarget = Helpers.DefaultTypeMapping.TryGetValue(e.Name, out var mapping) ? mapping.Name : null
            })
            .ToList();
    }

    public List<GraphqlType> GetCustomScalars()
    {
        var mappers = Helpers.EffectiveTypeMapping;
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