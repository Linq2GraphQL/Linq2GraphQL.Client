namespace Linq2GraphQL.Client.AI.Schema;

internal enum SchemaTypeKind
{
    Object,
    Interface,
    Union,
    Enum,
    InputObject,
    Scalar
}

/// <summary>
/// A type reference as written in the schema: a named type, possibly wrapped in lists and non-null markers.
/// </summary>
internal sealed record TypeRef(string Name, TypeRef OfType, bool IsList, bool IsNonNull)
{
    public static TypeRef Named(string name) => new(name, null, false, false);
    public static TypeRef List(TypeRef ofType) => new(null, ofType, true, false);
    public static TypeRef NonNull(TypeRef ofType) => new(null, ofType, false, true);

    /// <summary>
    /// The named type at the bottom of the wrappers.
    /// </summary>
    public string NamedType => OfType == null ? Name : OfType.NamedType;

    public override string ToString() =>
        IsNonNull ? $"{OfType}!" : IsList ? $"[{OfType}]" : Name;
}

internal sealed class SchemaArgument
{
    public required string Name { get; init; }
    public string Description { get; set; }
    public required TypeRef Type { get; init; }
    public string DefaultValue { get; init; }
}

internal sealed class SchemaField
{
    public required string Name { get; init; }
    public string Description { get; set; }
    public required TypeRef Type { get; init; }
    public List<SchemaArgument> Arguments { get; init; } = [];
    public string DefaultValue { get; init; }
    public bool IsDeprecated { get; init; }

    public SchemaField Copy() => new()
    {
        Name = Name,
        Description = Description,
        Type = Type,
        Arguments = Arguments.Select(e => new SchemaArgument
        {
            Name = e.Name, Description = e.Description, Type = e.Type, DefaultValue = e.DefaultValue
        }).ToList(),
        DefaultValue = DefaultValue,
        IsDeprecated = IsDeprecated
    };
}

internal sealed class SchemaEnumValue
{
    public required string Name { get; init; }
    public string Description { get; set; }
    public bool IsDeprecated { get; init; }
}

internal sealed class SchemaType
{
    public required string Name { get; init; }
    public required SchemaTypeKind Kind { get; init; }
    public string Description { get; set; }

    /// <summary>
    /// Output fields of object and interface types, or input fields of input types, in schema order.
    /// </summary>
    public List<SchemaField> Fields { get; init; } = [];

    public List<SchemaEnumValue> EnumValues { get; init; } = [];
    public List<string> Interfaces { get; init; } = [];

    /// <summary>
    /// Member types of a union, or implementing object types of an interface.
    /// </summary>
    public List<string> PossibleTypes { get; init; } = [];

    public SchemaField Field(string name) => Fields.FirstOrDefault(e => e.Name == name);
}

/// <summary>
/// An example for a common task, as the agent sees it: GraphQL text, variables as JSON, and the root
/// operations it uses.
/// </summary>
internal sealed record AgentExample(string Name, string Description, string Query, string Variables,
    IReadOnlyList<string> Operations);

internal sealed class SchemaModel
{
    public required Dictionary<string, SchemaType> Types { get; init; }
    public required string QueryType { get; init; }
    public string MutationType { get; init; }
    public string SubscriptionType { get; init; }

    public SchemaType Type(string name) => Types.GetValueOrDefault(name);
}
