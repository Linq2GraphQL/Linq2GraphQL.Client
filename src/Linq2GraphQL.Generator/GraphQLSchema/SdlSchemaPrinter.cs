using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace Linq2GraphQL.Generator;

/// <summary>
/// Prints the schema model as SDL by building a GraphQL-Parser syntax tree and handing it to its SDL printer,
/// so escaping and block strings are handled the same way the parser reads them back. Introspection types
/// and the built-in scalars are left out, as SDL leaves them implicit.
/// </summary>
public static class SdlSchemaPrinter
{
    private static readonly HashSet<string> BuiltInScalars = ["String", "Int", "Float", "Boolean", "ID"];

    public static string Print(Schema schema)
    {
        var definitions = new List<ASTNode>();

        if (SchemaDefinition(schema) is { } schemaDefinition)
        {
            definitions.Add(schemaDefinition);
        }

        foreach (var type in schema.Types.Where(e => !e.Name.StartsWith("__") && !BuiltInScalars.Contains(e.Name)))
        {
            if (TypeDefinition(type) is { } definition)
            {
                definitions.Add(definition);
            }
        }

        return new SDLPrinter().Print(new GraphQLDocument(definitions)).ReplaceLineEndings("\n").TrimEnd() + "\n";
    }

    /// <summary>
    /// A schema block is only needed when a root type is not named Query, Mutation or Subscription.
    /// </summary>
    private static GraphQLSchemaDefinition SchemaDefinition(Schema schema)
    {
        var roots = new List<(OperationType Operation, string Name, string DefaultName)>
        {
            (OperationType.Query, schema.SchemaQueryType?.Name, "Query"),
            (OperationType.Mutation, schema.SchemaMutationType?.Name, "Mutation"),
            (OperationType.Subscription, schema.SchemaSubscriptionType?.Name, "Subscription")
        };

        if (roots.All(e => e.Name == null || e.Name == e.DefaultName))
        {
            return null;
        }

        return new GraphQLSchemaDefinition(roots.Where(e => e.Name != null)
            .Select(e => new GraphQLRootOperationTypeDefinition { Operation = e.Operation, Type = NamedType(e.Name) })
            .ToList());
    }

    private static ASTNode TypeDefinition(GraphqlType type)
    {
        return type.Kind switch
        {
            TypeKind.Object => new GraphQLObjectTypeDefinition(Name(type.Name))
            {
                Description = Description(type.Description),
                Interfaces = Interfaces(type),
                Fields = Fields(type)
            },
            TypeKind.Interface => new GraphQLInterfaceTypeDefinition(Name(type.Name))
            {
                Description = Description(type.Description),
                Interfaces = Interfaces(type),
                Fields = Fields(type)
            },
            TypeKind.InputObject => new GraphQLInputObjectTypeDefinition(Name(type.Name))
            {
                Description = Description(type.Description),
                Fields = type.InputFields is { Count: > 0 }
                    ? new GraphQLInputFieldsDefinition(type.InputFields.Select(e => InputValue(e, isArgument: false)).ToList())
                    : null
            },
            TypeKind.Enum => new GraphQLEnumTypeDefinition(Name(type.Name))
            {
                Description = Description(type.Description),
                Values = type.EnumValues is { Count: > 0 }
                    ? new GraphQLEnumValuesDefinition(type.EnumValues.Select(e =>
                        new GraphQLEnumValueDefinition(Name(e.Name), new GraphQLEnumValue(Name(e.Name)))
                        {
                            Description = Description(e.Description),
                            Directives = Deprecated(e.IsDeprecated, e.DeprecationReason)
                        }).ToList())
                    : null
            },
            TypeKind.Union => new GraphQLUnionTypeDefinition(Name(type.Name))
            {
                Description = Description(type.Description),
                Types = type.PossibleTypes is { Count: > 0 }
                    ? new GraphQLUnionMemberTypes(type.PossibleTypes.Select(e => NamedType(e.Name)).ToList())
                    : null
            },
            TypeKind.Scalar => new GraphQLScalarTypeDefinition(Name(type.Name))
            {
                Description = Description(type.Description)
            },
            _ => null
        };
    }

    private static GraphQLImplementsInterfaces Interfaces(GraphqlType type)
    {
        return type.Interfaces is { Count: > 0 }
            ? new GraphQLImplementsInterfaces(type.Interfaces.Select(e => NamedType(e.Name)).ToList())
            : null;
    }

    private static GraphQLFieldsDefinition Fields(GraphqlType type)
    {
        if (type.Fields is not { Count: > 0 })
        {
            return null;
        }

        return new GraphQLFieldsDefinition(type.Fields.Select(field =>
            new GraphQLFieldDefinition(Name(field.Name), TypeRef(field.Type))
            {
                Description = Description(field.Description),
                Arguments = field.Args is { Count: > 0 }
                    ? new GraphQLArgumentsDefinition(field.Args.Select(e => InputValue(e, isArgument: true)).ToList())
                    : null,
                Directives = Deprecated(field.IsDeprecated, field.DeprecationReason)
            }).ToList());
    }

    private static GraphQLInputValueDefinition InputValue(BaseField field, bool isArgument)
    {
        GraphQLInputValueDefinition definition = isArgument
            ? new GraphQLArgumentDefinition(Name(field.Name), TypeRef(field.Type))
            : new GraphQLInputFieldDefinition(Name(field.Name), TypeRef(field.Type));

        definition.Description = Description(field.Description);
        definition.DefaultValue = DefaultValue(field.DefaultValue);
        definition.Directives = Deprecated(field.IsDeprecated, field.DeprecationReason);
        return definition;
    }

    private static GraphQLType TypeRef(BaseType type)
    {
        return type.Kind switch
        {
            TypeKind.NonNull => new GraphQLNonNullType(TypeRef(type.OfType)),
            TypeKind.List => new GraphQLListType(TypeRef(type.OfType)),
            _ => NamedType(type.Name)
        };
    }

    private static GraphQLDirectives Deprecated(bool isDeprecated, string reason)
    {
        if (!isDeprecated)
        {
            return null;
        }

        var directive = new GraphQLDirective(Name("deprecated"));
        if (!string.IsNullOrEmpty(reason))
        {
            directive.Arguments = new GraphQLArguments([new GraphQLArgument(Name("reason"), new GraphQLStringValue(reason))]);
        }

        return new GraphQLDirectives([directive]);
    }

    /// <summary>
    /// Default values arrive as GraphQL literals (introspection reports them as text). They are parsed back
    /// into a value node so the printer writes them as the server did.
    /// </summary>
    private static GraphQLValue DefaultValue(string literal)
    {
        if (string.IsNullOrWhiteSpace(literal))
        {
            return null;
        }

        var document = Parser.Parse($"input DefaultValue {{ value: Int = {literal} }}");
        return ((GraphQLInputObjectTypeDefinition)document.Definitions[0]).Fields!.Items[0].DefaultValue;
    }

    private static GraphQLDescription Description(string text) =>
        string.IsNullOrEmpty(text) ? null : new GraphQLDescription(text);

    private static GraphQLNamedType NamedType(string name) => new(Name(name));

    private static GraphQLName Name(string name) => new(name);
}
