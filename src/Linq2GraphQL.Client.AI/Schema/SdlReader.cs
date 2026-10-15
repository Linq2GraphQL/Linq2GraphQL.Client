using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Exceptions;
using GraphQLParser.Visitors;

namespace Linq2GraphQL.Client.AI.Schema;

/// <summary>
/// Reads SDL into the agent package's own schema model. It accepts the SDL the generator emits in agent mode
/// as well as hand-written or exported schema files, including type extensions.
/// </summary>
internal static class SdlReader
{
    private static readonly string[] BuiltInScalars = ["String", "Int", "Float", "Boolean", "ID"];

    public static SchemaModel Read(string sdl)
    {
        GraphQLDocument document;
        try
        {
            document = Parser.Parse(sdl);
        }
        catch (GraphQLParserException ex)
        {
            throw new AgentConfigurationException($"The schema is not valid SDL: {ex.Message}", ex);
        }

        var types = new Dictionary<string, SchemaType>();
        foreach (var scalar in BuiltInScalars)
        {
            types[scalar] = new SchemaType { Name = scalar, Kind = SchemaTypeKind.Scalar };
        }

        foreach (var definition in document.Definitions)
        {
            var type = definition switch
            {
                GraphQLObjectTypeDefinition e => NewType(e.Name, SchemaTypeKind.Object, e.Description),
                GraphQLInterfaceTypeDefinition e => NewType(e.Name, SchemaTypeKind.Interface, e.Description),
                GraphQLUnionTypeDefinition e => NewType(e.Name, SchemaTypeKind.Union, e.Description),
                GraphQLEnumTypeDefinition e => NewType(e.Name, SchemaTypeKind.Enum, e.Description),
                GraphQLInputObjectTypeDefinition e => NewType(e.Name, SchemaTypeKind.InputObject, e.Description),
                GraphQLScalarTypeDefinition e => NewType(e.Name, SchemaTypeKind.Scalar, e.Description),
                _ => null
            };

            if (type != null)
            {
                types[type.Name] = type;
            }
        }

        foreach (var definition in document.Definitions)
        {
            switch (definition)
            {
                case GraphQLObjectTypeDefinition e:
                    AddMembers(types, e.Name, e.Interfaces, e.Fields);
                    break;
                case GraphQLObjectTypeExtension e:
                    AddMembers(types, e.Name, e.Interfaces, e.Fields);
                    break;
                case GraphQLInterfaceTypeDefinition e:
                    AddMembers(types, e.Name, e.Interfaces, e.Fields);
                    break;
                case GraphQLInterfaceTypeExtension e:
                    AddMembers(types, e.Name, e.Interfaces, e.Fields);
                    break;
                case GraphQLInputObjectTypeDefinition e:
                    AddInputFields(types, e.Name, e.Fields);
                    break;
                case GraphQLInputObjectTypeExtension e:
                    AddInputFields(types, e.Name, e.Fields);
                    break;
                case GraphQLEnumTypeDefinition e:
                    AddEnumValues(types, e.Name, e.Values);
                    break;
                case GraphQLEnumTypeExtension e:
                    AddEnumValues(types, e.Name, e.Values);
                    break;
                case GraphQLUnionTypeDefinition e:
                    Existing(types, e.Name).PossibleTypes.AddRange(e.Types?.Items.Select(t => Name(t.Name)) ?? []);
                    break;
                case GraphQLUnionTypeExtension e:
                    Existing(types, e.Name).PossibleTypes.AddRange(e.Types?.Items.Select(t => Name(t.Name)) ?? []);
                    break;
            }
        }

        foreach (var interfaceType in types.Values.Where(e => e.Kind == SchemaTypeKind.Interface))
        {
            interfaceType.PossibleTypes.AddRange(types.Values
                .Where(e => e.Kind == SchemaTypeKind.Object && e.Interfaces.Contains(interfaceType.Name))
                .Select(e => e.Name));
        }

        var schemaDefinition = document.Definitions.OfType<GraphQLSchemaDefinition>().FirstOrDefault();
        var model = new SchemaModel
        {
            Types = types,
            QueryType = RootType(schemaDefinition, types, GraphQLParser.AST.OperationType.Query, "Query")
                        ?? throw new AgentConfigurationException("The schema has no query type."),
            MutationType = RootType(schemaDefinition, types, GraphQLParser.AST.OperationType.Mutation, "Mutation"),
            SubscriptionType = RootType(schemaDefinition, types, GraphQLParser.AST.OperationType.Subscription, "Subscription")
        };

        var unknown = types.Values
            .SelectMany(t => t.Fields.SelectMany(f => f.Arguments.Select(a => a.Type).Append(f.Type)))
            .Select(e => e.NamedType)
            .FirstOrDefault(e => !types.ContainsKey(e));
        if (unknown != null)
        {
            throw new AgentConfigurationException($"The schema references type {unknown}, which is not defined.");
        }

        return model;
    }

    private static SchemaType NewType(GraphQLName name, SchemaTypeKind kind, GraphQLDescription description) =>
        new() { Name = Name(name), Kind = kind, Description = Text(description) };

    private static SchemaType Existing(Dictionary<string, SchemaType> types, GraphQLName name) =>
        types.TryGetValue(Name(name), out var type)
            ? type
            : throw new AgentConfigurationException($"The schema extends type {Name(name)}, which is not defined.");

    private static void AddMembers(Dictionary<string, SchemaType> types, GraphQLName name,
        GraphQLImplementsInterfaces interfaces, GraphQLFieldsDefinition fields)
    {
        var type = Existing(types, name);
        type.Interfaces.AddRange(interfaces?.Items.Select(e => Name(e.Name)) ?? []);
        type.Fields.AddRange((fields?.Items ?? []).Select(field => new SchemaField
        {
            Name = Name(field.Name),
            Description = Text(field.Description),
            Type = TypeRefOf(field.Type),
            IsDeprecated = IsDeprecated(field.Directives),
            Arguments = (field.Arguments?.Items ?? []).Select(e => new SchemaArgument
            {
                Name = Name(e.Name),
                Description = Text(e.Description),
                Type = TypeRefOf(e.Type),
                DefaultValue = Print(e.DefaultValue)
            }).ToList()
        }));
    }

    private static void AddInputFields(Dictionary<string, SchemaType> types, GraphQLName name,
        GraphQLInputFieldsDefinition fields)
    {
        Existing(types, name).Fields.AddRange((fields?.Items ?? []).Select(field => new SchemaField
        {
            Name = Name(field.Name),
            Description = Text(field.Description),
            Type = TypeRefOf(field.Type),
            DefaultValue = Print(field.DefaultValue),
            IsDeprecated = IsDeprecated(field.Directives)
        }));
    }

    private static void AddEnumValues(Dictionary<string, SchemaType> types, GraphQLName name,
        GraphQLEnumValuesDefinition values)
    {
        Existing(types, name).EnumValues.AddRange((values?.Items ?? []).Select(value => new SchemaEnumValue
        {
            Name = Name(value.EnumValue.Name),
            Description = Text(value.Description),
            IsDeprecated = IsDeprecated(value.Directives)
        }));
    }

    private static string RootType(GraphQLSchemaDefinition schemaDefinition, Dictionary<string, SchemaType> types,
        GraphQLParser.AST.OperationType operation, string defaultName)
    {
        if (schemaDefinition != null)
        {
            var root = schemaDefinition.OperationTypes.FirstOrDefault(e => e.Operation == operation);
            return root == null ? null : Name(root.Type.Name);
        }

        return types.ContainsKey(defaultName) ? defaultName : null;
    }

    private static TypeRef TypeRefOf(GraphQLType type) => type switch
    {
        GraphQLNonNullType e => TypeRef.NonNull(TypeRefOf(e.Type)),
        GraphQLListType e => TypeRef.List(TypeRefOf(e.Type)),
        GraphQLNamedType e => TypeRef.Named(Name(e.Name)),
        _ => throw new AgentConfigurationException($"Unsupported type reference {type.Kind}.")
    };

    private static bool IsDeprecated(GraphQLDirectives directives) =>
        directives?.Items.Any(e => Name(e.Name) == "deprecated") == true;

    private static string Print(GraphQLValue value) => value == null ? null : new SDLPrinter().Print(value);

    private static string Name(GraphQLName name) => name.StringValue;

    private static string Text(GraphQLDescription description) => description?.Value.ToString();
}
