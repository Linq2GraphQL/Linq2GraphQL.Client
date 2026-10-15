using System.Text.Json;
using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Exceptions;

namespace Linq2GraphQL.Client.AI.Schema;

/// <summary>
/// One problem with a query, at a path such as <c>orders(where).customer.status</c>.
/// </summary>
internal sealed record ValidationError(string Path, string Message)
{
    public override string ToString() => string.IsNullOrEmpty(Path) ? Message : $"{Path}: {Message}";
}

internal sealed class ValidationResult(IReadOnlyList<ValidationError> errors)
{
    public IReadOnlyList<ValidationError> Errors { get; } = errors;

    public bool IsValid => Errors.Count == 0;

    public override string ToString() => string.Join("\n", Errors);
}

/// <summary>
/// Checks a query the agent wrote against the scoped schema before it is sent: one query operation, allowed
/// roots, fields that exist and aren't omitted, arguments, input objects (filters included), enum values and
/// variables, with fragments followed through their type conditions. Aliases don't matter because the check
/// uses real field names. Variable values are checked too, so an omitted field cannot be passed in through
/// variables. All errors are returned at once, each saying where and what to do instead.
/// </summary>
internal sealed class QueryValidator(ScopedSchema schema)
{
    private const int MaxListedNames = 15;

    private readonly SchemaModel model = schema.Schema;
    private readonly SchemaShape shape = new(schema.Schema);

    public ValidationResult Validate(string query, JsonElement? variables = null)
    {
        var run = new Run(this);
        run.Validate(query, variables);
        return new ValidationResult(run.Errors);
    }

    private sealed class Run(QueryValidator validator)
    {
        private readonly SchemaModel model = validator.model;
        private readonly SchemaShape shape = validator.shape;
        private readonly Dictionary<string, GraphQLFragmentDefinition> fragments = new();
        private readonly Dictionary<string, TypeRef> declaredVariables = new();

        public List<ValidationError> Errors { get; } = [];

        public void Validate(string query, JsonElement? variables)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                Error("", "The query is empty.");
                return;
            }

            GraphQLDocument document;
            try
            {
                document = Parser.Parse(query);
            }
            catch (GraphQLParserException ex)
            {
                Error("", $"Syntax error: {ex.Message}");
                return;
            }

            foreach (var fragment in document.Definitions.OfType<GraphQLFragmentDefinition>())
            {
                fragments[Name(fragment.FragmentName.Name)] = fragment;
            }

            var operations = document.Definitions.OfType<GraphQLOperationDefinition>().ToList();
            if (document.Definitions.Any(e => e is not (GraphQLOperationDefinition or GraphQLFragmentDefinition)))
            {
                Error("", "Only a query and its fragments can be sent; schema definitions are not allowed.");
            }

            if (operations.Count != 1)
            {
                Error("", operations.Count == 0
                    ? "The document has no operation. Send one query, for example `query { ... }`."
                    : "Send one operation per call.");
                return;
            }

            var operation = operations[0];
            if (operation.Operation != GraphQLParser.AST.OperationType.Query)
            {
                Error("", $"Only queries are allowed; this is a {operation.Operation.ToString().ToLowerInvariant()}.");
                return;
            }

            ValidateVariableDefinitions(operation, variables);
            ValidateSelections(model.Type(model.QueryType), operation.SelectionSet, "", []);
        }

        private void ValidateVariableDefinitions(GraphQLOperationDefinition operation, JsonElement? variables)
        {
            foreach (var definition in operation.Variables?.Items ?? [])
            {
                var name = Name(definition.Variable.Name);
                var type = TypeRefOf(definition.Type);
                var path = "$" + name;

                if (model.Type(type.NamedType) is not { } namedType)
                {
                    Error(path, $"Type {type.NamedType} is not available.{Suggest(type.NamedType, InputTypeNames())}");
                    continue;
                }

                if (namedType.Kind is not (SchemaTypeKind.Scalar or SchemaTypeKind.Enum or SchemaTypeKind.InputObject))
                {
                    Error(path, $"{namedType.Name} is an output type and cannot be a variable.");
                    continue;
                }

                declaredVariables[name] = type;

                if (definition.DefaultValue != null)
                {
                    ValidateValue(definition.DefaultValue, type, path);
                }

                if (variables is { ValueKind: JsonValueKind.Object } values && values.TryGetProperty(name, out var value))
                {
                    ValidateJson(value, type, path);
                }
                else if (type.IsNonNull && definition.DefaultValue == null)
                {
                    Error(path, $"Variable ${name} is required ({type}) but no value was given.");
                }
            }

            if (variables is { ValueKind: JsonValueKind.Object } given)
            {
                foreach (var property in given.EnumerateObject().Where(e => !declaredVariables.ContainsKey(e.Name)))
                {
                    Error("$" + property.Name, $"Variable ${property.Name} is not declared in the query.");
                }
            }
        }

        private void ValidateSelections(SchemaType parent, GraphQLSelectionSet selectionSet, string path,
            HashSet<string> fragmentsInUse)
        {
            foreach (var selection in selectionSet.Selections)
            {
                switch (selection)
                {
                    case GraphQLField field:
                        ValidateField(parent, field, path, fragmentsInUse);
                        break;
                    case GraphQLFragmentSpread spread:
                        ValidateFragmentSpread(parent, spread, path, fragmentsInUse);
                        break;
                    case GraphQLInlineFragment inline:
                        ValidateDirectives(inline.Directives, path);
                        var type = inline.TypeCondition == null
                            ? parent
                            : FragmentType(parent, Name(inline.TypeCondition.Type.Name), path);
                        if (type != null)
                        {
                            ValidateSelections(type, inline.SelectionSet, path, fragmentsInUse);
                        }

                        break;
                }
            }
        }

        private void ValidateFragmentSpread(SchemaType parent, GraphQLFragmentSpread spread, string path,
            HashSet<string> fragmentsInUse)
        {
            ValidateDirectives(spread.Directives, path);

            var name = Name(spread.FragmentName.Name);
            if (!fragments.TryGetValue(name, out var fragment))
            {
                Error(path, $"Fragment {name} is not defined.");
                return;
            }

            if (!fragmentsInUse.Add(name))
            {
                Error(path, $"Fragment {name} spreads itself.");
                return;
            }

            if (FragmentType(parent, Name(fragment.TypeCondition.Type.Name), path) is { } type)
            {
                ValidateSelections(type, fragment.SelectionSet, path, fragmentsInUse);
            }

            fragmentsInUse.Remove(name);
        }

        /// <summary>
        /// The type a fragment selects on, if it exists in scope and can apply where it is spread.
        /// </summary>
        private SchemaType FragmentType(SchemaType parent, string typeName, string path)
        {
            if (model.Type(typeName) is not { } type)
            {
                Error(path, $"Type {typeName} is not available.");
                return null;
            }

            var applies = type.Name == parent.Name ||
                          parent.PossibleTypes.Contains(type.Name) ||
                          type.PossibleTypes.Contains(parent.Name) ||
                          type.PossibleTypes.Intersect(parent.PossibleTypes).Any();
            if (!applies)
            {
                Error(path, $"A fragment on {type.Name} cannot apply to {parent.Name}.");
                return null;
            }

            return type;
        }

        private void ValidateField(SchemaType parent, GraphQLField field, string parentPath,
            HashSet<string> fragmentsInUse)
        {
            var name = Name(field.Name);
            var path = Join(parentPath, name);
            ValidateDirectives(field.Directives, path);

            if (name == "__typename")
            {
                return;
            }

            if (name is "__schema" or "__type")
            {
                Error(path, "Introspection is not available. Use describe_operation, describe_type and search_schema to explore the schema.");
                return;
            }

            var definition = parent.Field(name);
            if (definition == null)
            {
                Error(path, MissingField(parent, name));
                return;
            }

            ValidateArguments(definition, field, path);

            var type = model.Type(definition.Type.NamedType);
            var isLeaf = type.Kind is SchemaTypeKind.Scalar or SchemaTypeKind.Enum;

            if (isLeaf && field.SelectionSet != null)
            {
                Error(path, $"{name} is a {type.Name} and has no fields to select. Remove `{{ ... }}`.");
            }
            else if (!isLeaf && field.SelectionSet == null)
            {
                Error(path, $"{name} returns {SelectionHint(type)}; select fields, for example `{name} {{ {ExampleFields(type)} }}`.");
            }
            else if (!isLeaf)
            {
                ValidateSelections(type, field.SelectionSet, path, fragmentsInUse);
            }
        }

        private string MissingField(SchemaType parent, string name)
        {
            if (parent.Name == model.QueryType)
            {
                return $"Query {name} is not available.{Suggest(name, parent.Fields.Select(e => e.Name))}";
            }

            if (shape.ConnectionOf(parent.Name) is { } connection)
            {
                var pageField = connection.HasNodes ? "nodes" : "edges { node";
                var close = connection.HasNodes ? "}" : "} }";
                return $"This field is paged; select {name} inside `{pageField} {{ {name} {close}`, " +
                       "for example `{ nodes { ... } pageInfo { hasNextPage endCursor } }`.";
            }

            return parent.Kind == SchemaTypeKind.Union
                ? $"{parent.Name} is a union; select fields inside `... on {string.Join(" / ", parent.PossibleTypes)} {{ ... }}`."
                : $"{parent.Name} has no field {name}.{Suggest(name, parent.Fields.Select(e => e.Name))}";
        }

        private void ValidateArguments(SchemaField definition, GraphQLField field, string path)
        {
            var given = field.Arguments?.Items ?? [];
            foreach (var argument in given)
            {
                var name = Name(argument.Name);
                var argumentPath = $"{path}({name})";
                var argumentDefinition = definition.Arguments.FirstOrDefault(e => e.Name == name);
                if (argumentDefinition == null)
                {
                    Error(argumentPath, $"{definition.Name} has no argument {name}." +
                                        Suggest(name, definition.Arguments.Select(e => e.Name)));
                    continue;
                }

                ValidateValue(argument.Value, argumentDefinition.Type, argumentPath);
            }

            foreach (var required in definition.Arguments.Where(e =>
                         e.Type.IsNonNull && e.DefaultValue == null && given.All(g => Name(g.Name) != e.Name)))
            {
                Error(path, $"Argument {required.Name} ({required.Type}) is required.");
            }
        }

        private void ValidateDirectives(GraphQLDirectives directives, string path)
        {
            foreach (var directive in directives?.Items ?? [])
            {
                var name = Name(directive.Name);
                if (name is not ("include" or "skip"))
                {
                    Error(path, $"Directive @{name} is not supported; only @include and @skip are.");
                }
            }
        }

        private void ValidateValue(GraphQLValue value, TypeRef expected, string path)
        {
            if (value is GraphQLVariable variable)
            {
                var name = Name(variable.Name);
                if (!declaredVariables.TryGetValue(name, out var declared))
                {
                    Error(path, $"Variable ${name} is not declared. Declare it, for example `query(${name}: {expected})`.");
                }
                else if (declared.NamedType != expected.NamedType)
                {
                    Error(path, $"Variable ${name} is declared as {declared} but {expected} is expected here.");
                }

                return;
            }

            if (value is GraphQLNullValue)
            {
                if (expected.IsNonNull)
                {
                    Error(path, $"A value is required ({expected}).");
                }

                return;
            }

            if (expected.IsNonNull)
            {
                ValidateValue(value, expected.OfType, path);
                return;
            }

            if (expected.IsList)
            {
                if (value is GraphQLListValue list)
                {
                    var index = 0;
                    foreach (var item in list.Values ?? [])
                    {
                        ValidateValue(item, expected.OfType, $"{path}[{index++}]");
                    }
                }
                else
                {
                    ValidateValue(value, expected.OfType, path);
                }

                return;
            }

            var type = model.Type(expected.Name);
            switch (type.Kind)
            {
                case SchemaTypeKind.InputObject when value is GraphQLObjectValue objectValue:
                    var fields = objectValue.Fields ?? [];
                    foreach (var field in fields)
                    {
                        ValidateInputField(type, Name(field.Name), path, fieldPath => ValidateValue(field.Value, type.Field(Name(field.Name)).Type, fieldPath));
                    }

                    RequireInputFields(type, fields.Select(e => Name(e.Name)), path);
                    break;
                case SchemaTypeKind.InputObject:
                    Error(path, $"Expected an object for {type.Name}, for example `{{ {ExampleFields(type)} }}`.");
                    break;
                case SchemaTypeKind.Enum when value is GraphQLEnumValue enumValue:
                    ValidateEnumValue(type, Name(enumValue.Name), path);
                    break;
                case SchemaTypeKind.Enum when value is GraphQLStringValue text:
                    Error(path, $"{type.Name} values are written without quotes, for example `{text.Value.ToString()}`." +
                                Suggest(text.Value.ToString(), type.EnumValues.Select(e => e.Name)));
                    break;
                case SchemaTypeKind.Enum:
                    Error(path, $"Expected a {type.Name} value: {Names(type.EnumValues.Select(e => e.Name))}.");
                    break;
                case SchemaTypeKind.Scalar when !ScalarAccepts(type.Name, value):
                    Error(path, $"Expected a {type.Name} value.");
                    break;
            }
        }

        private void ValidateJson(JsonElement value, TypeRef expected, string path)
        {
            if (value.ValueKind == JsonValueKind.Null)
            {
                if (expected.IsNonNull)
                {
                    Error(path, $"A value is required ({expected}).");
                }

                return;
            }

            if (expected.IsNonNull)
            {
                ValidateJson(value, expected.OfType, path);
                return;
            }

            if (expected.IsList)
            {
                if (value.ValueKind == JsonValueKind.Array)
                {
                    var index = 0;
                    foreach (var item in value.EnumerateArray())
                    {
                        ValidateJson(item, expected.OfType, $"{path}[{index++}]");
                    }
                }
                else
                {
                    ValidateJson(value, expected.OfType, path);
                }

                return;
            }

            var type = model.Type(expected.Name);
            switch (type.Kind)
            {
                case SchemaTypeKind.InputObject when value.ValueKind == JsonValueKind.Object:
                    var names = new List<string>();
                    foreach (var property in value.EnumerateObject())
                    {
                        names.Add(property.Name);
                        ValidateInputField(type, property.Name, path, fieldPath => ValidateJson(property.Value, type.Field(property.Name).Type, fieldPath));
                    }

                    RequireInputFields(type, names, path);
                    break;
                case SchemaTypeKind.InputObject:
                    Error(path, $"Expected an object for {type.Name}.");
                    break;
                case SchemaTypeKind.Enum when value.ValueKind == JsonValueKind.String:
                    ValidateEnumValue(type, value.GetString(), path);
                    break;
                case SchemaTypeKind.Enum:
                    Error(path, $"Expected a {type.Name} value as a string: {Names(type.EnumValues.Select(e => e.Name))}.");
                    break;
                case SchemaTypeKind.Scalar when !ScalarAccepts(type.Name, value.ValueKind):
                    Error(path, $"Expected a {type.Name} value.");
                    break;
            }
        }

        private void ValidateInputField(SchemaType type, string name, string path, Action<string> validateValue)
        {
            var fieldPath = Join(path, name);
            if (type.Field(name) == null)
            {
                Error(fieldPath, $"{type.Name} has no field {name}.{Suggest(name, type.Fields.Select(e => e.Name))}");
                return;
            }

            validateValue(fieldPath);
        }

        private void RequireInputFields(SchemaType type, IEnumerable<string> given, string path)
        {
            var givenNames = given.ToHashSet();
            foreach (var required in type.Fields.Where(e =>
                         e.Type.IsNonNull && e.DefaultValue == null && !givenNames.Contains(e.Name)))
            {
                Error(path, $"{type.Name}.{required.Name} ({required.Type}) is required.");
            }
        }

        private void ValidateEnumValue(SchemaType type, string value, string path)
        {
            if (type.EnumValues.All(e => e.Name != value))
            {
                Error(path, $"{type.Name} has no value {value}.{Suggest(value, type.EnumValues.Select(e => e.Name))}");
            }
        }

        private static bool ScalarAccepts(string scalar, GraphQLValue value) => scalar switch
        {
            "Int" => value is GraphQLIntValue,
            "Float" => value is GraphQLIntValue or GraphQLFloatValue,
            "Boolean" => value is GraphQLBooleanValue,
            "String" => value is GraphQLStringValue,
            "ID" => value is GraphQLStringValue or GraphQLIntValue,
            _ => value is not (GraphQLObjectValue or GraphQLListValue)
        };

        private static bool ScalarAccepts(string scalar, JsonValueKind kind) => scalar switch
        {
            "Int" or "Float" => kind == JsonValueKind.Number,
            "Boolean" => kind is JsonValueKind.True or JsonValueKind.False,
            "String" => kind == JsonValueKind.String,
            "ID" => kind is JsonValueKind.String or JsonValueKind.Number,
            _ => kind is not (JsonValueKind.Object or JsonValueKind.Array)
        };

        private string SelectionHint(SchemaType type) =>
            shape.ConnectionOf(type.Name) is { } connection ? $"a page of {connection.NodeType}" : type.Name;

        private string ExampleFields(SchemaType type)
        {
            if (shape.ConnectionOf(type.Name) is { } connection)
            {
                var node = model.Type(connection.NodeType);
                return connection.HasNodes
                    ? $"nodes {{ {ExampleFields(node)} }} pageInfo {{ hasNextPage endCursor }}"
                    : $"edges {{ node {{ {ExampleFields(node)} }} }} pageInfo {{ hasNextPage endCursor }}";
            }

            if (type.Kind == SchemaTypeKind.Union)
            {
                return $"... on {type.PossibleTypes.FirstOrDefault()} {{ ... }}";
            }

            var leaves = type.Fields
                .Where(e => e.Arguments.Count == 0 &&
                            model.Type(e.Type.NamedType).Kind is SchemaTypeKind.Scalar or SchemaTypeKind.Enum)
                .Take(3)
                .Select(e => e.Name)
                .ToList();
            return leaves.Count > 0 ? string.Join(" ", leaves) : "...";
        }

        private IEnumerable<string> InputTypeNames() => model.Types.Values
            .Where(e => e.Kind is SchemaTypeKind.Scalar or SchemaTypeKind.Enum or SchemaTypeKind.InputObject)
            .Select(e => e.Name);

        private void Error(string path, string message) => Errors.Add(new ValidationError(path, message));
    }

    /// <summary>
    /// " Did you mean `x`?" for a near miss, and the available names, so the agent can fix the query in one go.
    /// </summary>
    private static string Suggest(string name, IEnumerable<string> candidates)
    {
        var list = candidates.ToList();
        var closest = list
            .Select(e => (Name: e, Distance: Distance(name.ToLowerInvariant(), e.ToLowerInvariant())))
            .Where(e => e.Distance <= Math.Max(2, name.Length / 3))
            .OrderBy(e => e.Distance)
            .Select(e => e.Name)
            .FirstOrDefault();

        var suggestion = closest == null ? "" : $" Did you mean `{closest}`?";
        return list.Count == 0 ? suggestion : $"{suggestion} Available: {Names(list)}.";
    }

    private static string Names(IEnumerable<string> names)
    {
        var list = names.ToList();
        return list.Count <= MaxListedNames
            ? string.Join(", ", list)
            : string.Join(", ", list.Take(MaxListedNames)) + $" and {list.Count - MaxListedNames} more";
    }

    private static int Distance(string a, string b)
    {
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var current = new int[b.Length + 1];
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }

            previous = current;
        }

        return previous[b.Length];
    }

    private static string Join(string path, string name) => string.IsNullOrEmpty(path) ? name : $"{path}.{name}";

    private static string Name(GraphQLName name) => name.StringValue;

    private static TypeRef TypeRefOf(GraphQLType type) => type switch
    {
        GraphQLNonNullType e => TypeRef.NonNull(TypeRefOf(e.Type)),
        GraphQLListType e => TypeRef.List(TypeRefOf(e.Type)),
        GraphQLNamedType e => TypeRef.Named(Name(e.Name)),
        _ => TypeRef.Named(type.Kind.ToString())
    };
}
