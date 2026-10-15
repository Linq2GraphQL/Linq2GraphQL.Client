using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Exceptions;
using GraphQLParser.Visitors;

namespace Linq2GraphQL.Generator;

/// <summary>
/// Reads a schema written in SDL into the same <see cref="RootSchema"/> model an introspection query
/// produces, so the templates don't need to know where the schema came from. Deprecated fields, input
/// fields and enum values are dropped unless <c>includeDeprecated</c> is set, mirroring the
/// introspection query that runs without <c>includeDeprecated: true</c>.
/// </summary>
public static class SdlSchemaReader
{
    private static readonly string[] BuiltInScalars = ["String", "Int", "Float", "Boolean", "ID"];

    public static RootSchema Read(string sdl, bool includeDeprecated)
    {
        GraphQLDocument document;
        try
        {
            document = Parser.Parse(sdl);
        }
        catch (GraphQLParserException ex)
        {
            throw new GeneratorConfigurationException($"The schema file is not valid SDL: {ex.Message}", ex);
        }

        var definitions = document.Definitions;
        var kinds = TypeKinds(definitions);
        var types = new Dictionary<string, GraphqlType>();

        foreach (var definition in definitions)
        {
            switch (definition)
            {
                case GraphQLObjectTypeDefinition objectType:
                    AddType(types, objectType.Name, TypeKind.Object, objectType.Description);
                    break;
                case GraphQLInterfaceTypeDefinition interfaceType:
                    AddType(types, interfaceType.Name, TypeKind.Interface, interfaceType.Description);
                    break;
                case GraphQLInputObjectTypeDefinition inputType:
                    AddType(types, inputType.Name, TypeKind.InputObject, inputType.Description);
                    break;
                case GraphQLEnumTypeDefinition enumType:
                    AddType(types, enumType.Name, TypeKind.Enum, enumType.Description);
                    break;
                case GraphQLUnionTypeDefinition unionType:
                    AddType(types, unionType.Name, TypeKind.Union, unionType.Description);
                    break;
                case GraphQLScalarTypeDefinition scalarType:
                    AddType(types, scalarType.Name, TypeKind.Scalar, scalarType.Description);
                    break;
            }
        }

        // Members are read after every type exists, so definitions and extensions can come in any order.
        foreach (var definition in definitions)
        {
            switch (definition)
            {
                case GraphQLObjectTypeDefinition objectType:
                    AddMembers(types[Name(objectType.Name)], objectType.Interfaces, objectType.Fields, kinds,
                        includeDeprecated);
                    break;
                case GraphQLInterfaceTypeDefinition interfaceType:
                    AddMembers(types[Name(interfaceType.Name)], interfaceType.Interfaces, interfaceType.Fields, kinds,
                        includeDeprecated);
                    break;
                case GraphQLObjectTypeExtension objectExtension:
                    AddMembers(ExtendedType(types, objectExtension.Name), objectExtension.Interfaces,
                        objectExtension.Fields, kinds, includeDeprecated);
                    break;
                case GraphQLInterfaceTypeExtension interfaceExtension:
                    AddMembers(ExtendedType(types, interfaceExtension.Name), interfaceExtension.Interfaces,
                        interfaceExtension.Fields, kinds, includeDeprecated);
                    break;
                case GraphQLInputObjectTypeDefinition inputType:
                    AddInputFields(types[Name(inputType.Name)], inputType.Fields, kinds, includeDeprecated);
                    break;
                case GraphQLInputObjectTypeExtension inputExtension:
                    AddInputFields(ExtendedType(types, inputExtension.Name), inputExtension.Fields, kinds,
                        includeDeprecated);
                    break;
                case GraphQLEnumTypeDefinition enumType:
                    AddEnumValues(types[Name(enumType.Name)], enumType.Values, includeDeprecated);
                    break;
                case GraphQLEnumTypeExtension enumExtension:
                    AddEnumValues(ExtendedType(types, enumExtension.Name), enumExtension.Values, includeDeprecated);
                    break;
                case GraphQLUnionTypeDefinition unionType:
                    AddPossibleTypes(types[Name(unionType.Name)], unionType.Types);
                    break;
                case GraphQLUnionTypeExtension unionExtension:
                    AddPossibleTypes(ExtendedType(types, unionExtension.Name), unionExtension.Types);
                    break;
            }
        }

        AddInterfaceImplementations(types);
        AddReferencedBuiltInScalars(types);

        var schema = new Schema
        {
            Types = types.Values.ToList(),
            SchemaQueryType = RootType(definitions, types, OperationType.Query, "Query"),
            SchemaMutationType = RootType(definitions, types, OperationType.Mutation, "Mutation"),
            SchemaSubscriptionType = RootType(definitions, types, OperationType.Subscription, "Subscription")
        };

        if (schema.SchemaQueryType == null)
        {
            throw new GeneratorConfigurationException(
                "The schema file has no query type. Define a type named Query or declare it in a schema block.");
        }

        return new RootSchema { Data = new Data { Schema = schema } };
    }

    private static Dictionary<string, TypeKind> TypeKinds(IEnumerable<ASTNode> definitions)
    {
        var kinds = BuiltInScalars.ToDictionary(e => e, _ => TypeKind.Scalar);

        foreach (var definition in definitions)
        {
            var (name, kind) = definition switch
            {
                GraphQLObjectTypeDefinition e => (e.Name, TypeKind.Object),
                GraphQLInterfaceTypeDefinition e => (e.Name, TypeKind.Interface),
                GraphQLInputObjectTypeDefinition e => (e.Name, TypeKind.InputObject),
                GraphQLEnumTypeDefinition e => (e.Name, TypeKind.Enum),
                GraphQLUnionTypeDefinition e => (e.Name, TypeKind.Union),
                GraphQLScalarTypeDefinition e => (e.Name, TypeKind.Scalar),
                _ => (null, TypeKind.NotSet)
            };

            if (name is not null)
            {
                kinds[Name(name)] = kind;
            }
        }

        return kinds;
    }

    private static void AddType(Dictionary<string, GraphqlType> types, GraphQLName name, TypeKind kind,
        GraphQLDescription description)
    {
        var typeName = Name(name);
        if (!types.TryAdd(typeName, new GraphqlType { Name = typeName, Kind = kind, Description = Text(description) }))
        {
            throw new GeneratorConfigurationException($"The schema file defines type {typeName} more than once.");
        }
    }

    private static GraphqlType ExtendedType(Dictionary<string, GraphqlType> types, GraphQLName name)
    {
        var typeName = Name(name);
        if (!types.TryGetValue(typeName, out var type))
        {
            throw new GeneratorConfigurationException($"The schema file extends type {typeName}, which is not defined.");
        }

        return type;
    }

    private static void AddMembers(GraphqlType type, GraphQLImplementsInterfaces interfaces,
        GraphQLFieldsDefinition fields, Dictionary<string, TypeKind> kinds, bool includeDeprecated)
    {
        type.Interfaces ??= [];
        type.Fields ??= [];

        foreach (var implemented in interfaces?.Items ?? [])
        {
            type.Interfaces.Add(new GraphqlType { Name = Name(implemented.Name), Kind = TypeKind.Interface });
        }

        foreach (var field in fields?.Items ?? [])
        {
            var (isDeprecated, reason) = Deprecation(field.Directives);
            if (isDeprecated && !includeDeprecated)
            {
                continue;
            }

            type.Fields.Add(new Field
            {
                Name = Name(field.Name),
                Description = Text(field.Description),
                IsDeprecated = isDeprecated,
                DeprecationReason = reason,
                Type = TypeRef(field.Type, kinds),
                Args = (field.Arguments?.Items ?? []).Select(e => new Arg
                {
                    Name = Name(e.Name),
                    Description = Text(e.Description),
                    DefaultValue = DefaultValue(e.DefaultValue),
                    Type = TypeRef(e.Type, kinds)
                }).ToList()
            });
        }
    }

    private static void AddInputFields(GraphqlType type, GraphQLInputFieldsDefinition fields,
        Dictionary<string, TypeKind> kinds, bool includeDeprecated)
    {
        type.InputFields ??= [];

        foreach (var field in fields?.Items ?? [])
        {
            var (isDeprecated, reason) = Deprecation(field.Directives);
            if (isDeprecated && !includeDeprecated)
            {
                continue;
            }

            type.InputFields.Add(new Field
            {
                Name = Name(field.Name),
                Description = Text(field.Description),
                IsDeprecated = isDeprecated,
                DeprecationReason = reason,
                DefaultValue = DefaultValue(field.DefaultValue),
                Type = TypeRef(field.Type, kinds)
            });
        }
    }

    private static void AddEnumValues(GraphqlType type, GraphQLEnumValuesDefinition values, bool includeDeprecated)
    {
        type.EnumValues ??= [];

        foreach (var value in values?.Items ?? [])
        {
            var (isDeprecated, reason) = Deprecation(value.Directives);
            if (isDeprecated && !includeDeprecated)
            {
                continue;
            }

            type.EnumValues.Add(new EnumValue
            {
                Name = Name(value.EnumValue.Name),
                Description = Text(value.Description),
                IsDeprecated = isDeprecated,
                DeprecationReason = reason
            });
        }
    }

    private static void AddPossibleTypes(GraphqlType type, GraphQLUnionMemberTypes members)
    {
        type.PossibleTypes ??= [];
        foreach (var member in members?.Items ?? [])
        {
            type.PossibleTypes.Add(new GraphqlType { Name = Name(member.Name), Kind = TypeKind.Object });
        }
    }

    /// <summary>
    /// Introspection reports the implementing object types of an interface as its possible types.
    /// </summary>
    private static void AddInterfaceImplementations(Dictionary<string, GraphqlType> types)
    {
        foreach (var interfaceType in types.Values.Where(e => e.Kind == TypeKind.Interface))
        {
            interfaceType.PossibleTypes = types.Values
                .Where(e => e.Kind == TypeKind.Object && e.Interfaces?.Any(i => i.Name == interfaceType.Name) == true)
                .Select(e => new GraphqlType { Name = e.Name, Kind = TypeKind.Object })
                .ToList();
        }
    }

    /// <summary>
    /// SDL leaves the built-in scalars implicit. Introspection lists the ones the schema uses, so they are
    /// added the same way: only when a field, argument or input field references them.
    /// </summary>
    private static void AddReferencedBuiltInScalars(Dictionary<string, GraphqlType> types)
    {
        var referenced = types.Values
            .SelectMany(e => (e.Fields ?? []).Concat(e.InputFields ?? []))
            .SelectMany(e => (e.Args ?? []).Select(a => a.Type).Append(e.Type))
            .Select(NamedTypeName)
            .ToHashSet();

        foreach (var scalar in BuiltInScalars.Where(e => referenced.Contains(e) && !types.ContainsKey(e)))
        {
            types[scalar] = new GraphqlType { Name = scalar, Kind = TypeKind.Scalar };
        }
    }

    private static string NamedTypeName(BaseType type) => type.OfType == null ? type.Name : NamedTypeName(type.OfType);

    private static GraphqlSchemaType RootType(IEnumerable<ASTNode> definitions, Dictionary<string, GraphqlType> types,
        OperationType operation, string defaultName)
    {
        var schemaDefinition = definitions.OfType<GraphQLSchemaDefinition>().FirstOrDefault();
        if (schemaDefinition != null)
        {
            var root = schemaDefinition.OperationTypes.FirstOrDefault(e => e.Operation == operation);
            return root == null ? null : new GraphqlSchemaType { Name = Name(root.Type.Name) };
        }

        return types.ContainsKey(defaultName) ? new GraphqlSchemaType { Name = defaultName } : null;
    }

    private static BaseType TypeRef(GraphQLType type, Dictionary<string, TypeKind> kinds)
    {
        return type switch
        {
            GraphQLNonNullType nonNull => new BaseType { Kind = TypeKind.NonNull, OfType = TypeRef(nonNull.Type, kinds) },
            GraphQLListType list => new BaseType { Kind = TypeKind.List, OfType = TypeRef(list.Type, kinds) },
            GraphQLNamedType named => new BaseType
            {
                Name = Name(named.Name),
                Kind = kinds.TryGetValue(Name(named.Name), out var kind)
                    ? kind
                    : throw new GeneratorConfigurationException(
                        $"The schema file references type {Name(named.Name)}, which is not defined.")
            },
            _ => throw new GeneratorConfigurationException($"Unsupported type reference {type.Kind}.")
        };
    }

    private static (bool IsDeprecated, string Reason) Deprecation(GraphQLDirectives directives)
    {
        var deprecated = directives?.Items.FirstOrDefault(e => Name(e.Name) == "deprecated");
        if (deprecated == null)
        {
            return (false, null);
        }

        var reason = deprecated.Arguments?.Items.FirstOrDefault(e => Name(e.Name) == "reason")?.Value;
        return (true, reason is GraphQLStringValue text ? text.Value.ToString() : "No longer supported");
    }

    private static string DefaultValue(GraphQLValue value)
    {
        return value == null ? null : new SDLPrinter().Print(value);
    }

    private static string Name(GraphQLName name) => name.StringValue;

    private static string Text(GraphQLDescription description) => description?.Value.ToString();
}
