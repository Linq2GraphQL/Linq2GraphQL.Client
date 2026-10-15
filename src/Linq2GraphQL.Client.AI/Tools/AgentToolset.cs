using System.Text;
using System.Text.Json;
using Linq2GraphQL.Client.AI.Schema;

namespace Linq2GraphQL.Client.AI.Tools;

/// <summary>
/// The four agent tools as plain methods, with no AI framework dependency: run a query, describe operations,
/// describe types and search the schema. Everything works from the scoped schema; only <see cref="QueryAsync"/>
/// talks to the server, and only after the query has passed validation. Results are text, because that is what
/// a tool hands back to the model.
/// </summary>
internal sealed class AgentToolset
{
    private const int StarterFieldCount = 6;
    private const int MaxSearchResults = 20;

    private static readonly JsonSerializerOptions ResultJson = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ScopedSchema schema;
    private readonly GraphClient client;
    private readonly QueryValidator validator;
    private readonly SchemaShape shape;
    private readonly SchemaModel model;
    private readonly IReadOnlyList<Action<AgentQueryLog>> queryLoggers;

    public AgentToolset(ScopedSchema schema, GraphClient client, IReadOnlyList<Action<AgentQueryLog>> queryLoggers = null)
    {
        this.schema = schema;
        this.client = client;
        this.queryLoggers = queryLoggers ?? [];
        validator = new QueryValidator(schema);
        shape = new SchemaShape(schema.Schema);
        model = schema.Schema;
        Index = SchemaRenderer.Render(schema, Detail.Index);
    }

    /// <summary>
    /// The Index: the allowed operations and type names, for the query tool's description.
    /// </summary>
    public string Index { get; }

    /// <summary>
    /// graphql_query: validates the query and its variables, and only sends a valid query. Returns JSON with
    /// <c>data</c> and <c>errors</c>, or <c>validationErrors</c> when nothing was sent.
    /// </summary>
    public async Task<string> QueryAsync(string query, JsonElement? variables = null,
        CancellationToken cancellationToken = default)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        var validation = validator.Validate(query, variables);
        if (!validation.IsValid)
        {
            var validationErrors = validation.Errors.Select(e => e.ToString()).ToList();
            Log(query, variables, validationErrors, [], started.Elapsed);
            return JsonSerializer.Serialize(new
            {
                validationErrors,
                note = "The query was not sent. Fix every error listed and try again."
            }, ResultJson);
        }

        try
        {
            var result = await client.ExecuteAsync(query, Variables(variables), cancellationToken);
            Log(query, variables, [], result.Errors?.Select(e => e.Message).ToList() ?? [], started.Elapsed);
            return JsonSerializer.Serialize(new
            {
                data = result.Data.ValueKind == JsonValueKind.Undefined ? (JsonElement?)null : result.Data,
                errors = result.Errors?.Select(e => new { message = e.Message, path = e.Path })
            }, ResultJson);
        }
        catch (GraphQueryRequestException ex)
        {
            Log(query, variables, [], [ex.Message], started.Elapsed);
            return JsonSerializer.Serialize(new { requestError = ex.Message }, ResultJson);
        }
    }

    /// <summary>
    /// Reports a query to the OnQuery loggers. A failing logger must not break the agent's call.
    /// </summary>
    private void Log(string query, JsonElement? variables, IReadOnlyList<string> validationErrors,
        IReadOnlyList<string> errors, TimeSpan duration)
    {
        if (queryLoggers.Count == 0)
        {
            return;
        }

        var entry = new AgentQueryLog(query, variables?.GetRawText(), validationErrors, errors, duration);
        foreach (var logger in queryLoggers)
        {
            try
            {
                logger(entry);
            }
            catch (Exception)
            {
                // Logging is best effort; the agent's query result matters more.
            }
        }
    }

    /// <summary>
    /// describe_operation: everything needed to call each named operation, including a starter query.
    /// </summary>
    public string DescribeOperations(IEnumerable<string> names)
    {
        var text = new StringBuilder();
        foreach (var name in names.Distinct())
        {
            if (text.Length > 0)
            {
                text.AppendLine();
            }

            var operation = schema.QueryType.Field(name);
            if (operation == null)
            {
                text.AppendLine($"Operation {name} is not available." +
                                NameSuggestions.Suggest(name, schema.QueryType.Fields.Select(e => e.Name)));
                continue;
            }

            DescribeOperation(text, operation);
        }

        return Finish(text);
    }

    /// <summary>
    /// describe_type: full detail for each named type, plus the types its fields use at Outline level. Filter
    /// and sort inputs are described by their summary line.
    /// </summary>
    public string DescribeTypes(IEnumerable<string> names)
    {
        var requested = names.Distinct().ToList();
        var text = new StringBuilder();
        var described = new List<string>();

        foreach (var name in requested)
        {
            var type = model.Type(name);
            if (type == null)
            {
                text.AppendLine($"Type {name} is not available." + NameSuggestions.Suggest(name, ListedTypeNames()));
            }
            else if (shape.IsEntityFilter(type))
            {
                text.AppendLine($"{type.Name} (filter): {SchemaRenderer.FilterSummary(model, shape, type)}");
            }
            else if (shape.IsSortInput(type))
            {
                text.AppendLine($"{type.Name} (sorting): {SchemaRenderer.SortSummary(model, shape, type)}");
            }
            else if (shape.ConnectionOf(type.Name) is { } connection)
            {
                text.AppendLine($"{type.Name} is a page of {connection.NodeType}; see the @paged convention. " +
                                $"Describe {connection.NodeType} for its fields.");
            }
            else if (SchemaRenderer.IsListed(type, shape))
            {
                described.Add(type.Name);
                text.Append(SchemaRenderer.RenderTypes(schema, [type.Name]));
            }
            else
            {
                text.AppendLine($"{type.Name} is a built-in scalar.");
            }
        }

        if (described.Count > 0)
        {
            var related = described
                .SelectMany(e => RelatedTypes(model.Type(e)))
                .Where(e => !described.Contains(e))
                .Distinct()
                .ToList();
            if (related.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("Related types:");
                text.Append(SchemaRenderer.RenderTypes(schema, related, Detail.Outline));
            }
        }

        return Finish(text);
    }

    /// <summary>
    /// search_schema: operations, examples, types, fields and enum values matching the text, names before descriptions.
    /// </summary>
    public string Search(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Give a word to search for, such as a field or type name.\n";
        }

        var term = text.Trim();
        var matches = new List<(int Score, int Kind, int Order, string Line)>();
        var order = 0;

        void Consider(int kind, string name, string description, string line)
        {
            var score = name.Equals(term, StringComparison.OrdinalIgnoreCase) ? 0
                : name.StartsWith(term, StringComparison.OrdinalIgnoreCase) ? 1
                : name.Contains(term, StringComparison.OrdinalIgnoreCase) ? 2
                : description?.Contains(term, StringComparison.OrdinalIgnoreCase) == true ? 3
                : -1;
            if (score >= 0)
            {
                matches.Add((score, kind, order++, line));
            }
        }

        foreach (var example in schema.Examples)
        {
            Consider(1, example.Name, example.Description, $"example {example.Name}  – {example.Description}");
        }

        foreach (var operation in schema.QueryType.Fields)
        {
            Consider(0, operation.Name, operation.Description,
                $"operation {operation.Name}: {SchemaRenderer.TypeText(operation.Type, shape)}{Summary(operation.Description)}");
        }

        foreach (var type in model.Types.Values.Where(e => e.Name != model.QueryType && SchemaRenderer.IsListed(e, shape)))
        {
            Consider(2, type.Name, type.Description, $"type {type.Name}{Summary(type.Description)}");

            foreach (var field in type.Fields)
            {
                Consider(3, field.Name, field.Description,
                    $"field {type.Name}.{field.Name}: {SchemaRenderer.TypeText(field.Type, shape)}{Summary(field.Description)}");
            }

            foreach (var value in type.EnumValues)
            {
                Consider(4, value.Name, value.Description, $"value {type.Name}.{value.Name}{Summary(value.Description)}");
            }
        }

        if (matches.Count == 0)
        {
            return $"No operations, types or fields match \"{term}\". Try a shorter or broader word.\n";
        }

        var result = new StringBuilder();
        foreach (var match in matches.OrderBy(e => e.Score).ThenBy(e => e.Kind).ThenBy(e => e.Order).Take(MaxSearchResults))
        {
            result.AppendLine(match.Line);
        }

        if (matches.Count > MaxSearchResults)
        {
            result.AppendLine($"… and {matches.Count - MaxSearchResults} more. Use a more specific word.");
        }

        return Finish(result);
    }

    private void DescribeOperation(StringBuilder text, SchemaField operation)
    {
        text.AppendLine($"{operation.Name}{Summary(operation.Description, full: true)}");

        if (operation.Arguments.Count > 0)
        {
            text.AppendLine("Arguments:");
            foreach (var argument in operation.Arguments)
            {
                var line = new StringBuilder($"  {argument.Name}: {argument.Type}");
                if (argument.DefaultValue != null)
                {
                    line.Append($" = {argument.DefaultValue}");
                }

                if (!string.IsNullOrWhiteSpace(argument.Description))
                {
                    line.Append($"  – {argument.Description.Trim().ReplaceLineEndings(" ")}");
                }

                var argumentType = model.Type(argument.Type.NamedType);
                if (shape.IsEntityFilter(argumentType))
                {
                    line.Append($"  – filterable: {SchemaRenderer.FilterSummary(model, shape, argumentType)}");
                }
                else if (shape.IsSortInput(argumentType))
                {
                    line.Append($"  – sortable: {SchemaRenderer.SortSummary(model, shape, argumentType)}");
                }

                text.AppendLine(line.ToString());
            }
        }

        text.AppendLine($"Returns: {SchemaRenderer.TypeText(operation.Type, shape)}");
        var returnType = ReturnedType(operation);
        if (SchemaRenderer.IsListed(returnType, shape) && returnType.Kind is not (SchemaTypeKind.Scalar or SchemaTypeKind.Enum))
        {
            foreach (var line in SchemaRenderer.RenderTypes(schema, [returnType.Name], Detail.Outline).TrimEnd().Split('\n'))
            {
                text.AppendLine("  " + line);
            }
        }

        text.AppendLine("Starter query:");
        text.AppendLine("  " + StarterQuery(operation));

        var examples = schema.Examples.Where(e => e.Operations.Contains(operation.Name)).ToList();
        if (examples.Count > 0)
        {
            text.AppendLine("Examples:");
            foreach (var example in examples)
            {
                text.AppendLine($"  {example.Name}  – {example.Description}");
                text.AppendLine("    " + example.Query.ReplaceLineEndings(" ").Trim());
                if (example.Variables != null)
                {
                    text.AppendLine("    variables: " + example.Variables);
                }
            }
        }
    }

    /// <summary>
    /// A valid query to adapt: required arguments as variables and the first few plain fields of the result.
    /// Paged operations get no page size, because the schema doesn't publish the server's maximum; the server
    /// then uses its own default.
    /// </summary>
    internal string StarterQuery(SchemaField operation)
    {
        var required = operation.Arguments.Where(e => e.Type.IsNonNull && e.DefaultValue == null).ToList();
        var declarations = required.Count == 0 ? "" : $"({string.Join(", ", required.Select(e => $"${e.Name}: {e.Type}"))})";
        var arguments = required.Select(e => $"{e.Name}: ${e.Name}").ToList();

        var call = arguments.Count == 0 ? operation.Name : $"{operation.Name}({string.Join(", ", arguments)})";
        return $"query{declarations} {{ {call}{Selection(model.Type(operation.Type.NamedType))} }}";
    }

    private string Selection(SchemaType type)
    {
        if (type.Kind is SchemaTypeKind.Scalar or SchemaTypeKind.Enum)
        {
            return "";
        }

        if (shape.ConnectionOf(type.Name) is { } connection)
        {
            var node = Selection(model.Type(connection.NodeType));
            return connection.HasNodes
                ? $" {{ nodes{node} pageInfo {{ hasNextPage endCursor }} }}"
                : $" {{ edges {{ node{node} }} pageInfo {{ hasNextPage endCursor }} }}";
        }

        if (type.Kind == SchemaTypeKind.Union)
        {
            var first = type.PossibleTypes.FirstOrDefault();
            return first == null ? " { __typename }" : $" {{ __typename ... on {first}{Selection(model.Type(first))} }}";
        }

        var fields = type.Fields
            .Where(e => e.Arguments.All(a => !a.Type.IsNonNull || a.DefaultValue != null) &&
                        model.Type(e.Type.NamedType).Kind is SchemaTypeKind.Scalar or SchemaTypeKind.Enum)
            .Take(StarterFieldCount)
            .Select(e => e.Name)
            .ToList();
        return $" {{ {(fields.Count == 0 ? "__typename" : string.Join(" ", fields))} }}";
    }

    private SchemaType ReturnedType(SchemaField operation) =>
        shape.ConnectionOf(operation.Type.NamedType) is { } connection
            ? model.Type(connection.NodeType)
            : model.Type(operation.Type.NamedType);

    /// <summary>
    /// The listed types a type's fields and arguments use, with connections resolved to their node types.
    /// </summary>
    private IEnumerable<string> RelatedTypes(SchemaType type) =>
        type.Fields
            .SelectMany(e => e.Arguments.Select(a => a.Type.NamedType).Append(e.Type.NamedType))
            .Select(e => shape.ConnectionOf(e)?.NodeType ?? e)
            .Concat(type.PossibleTypes)
            .Concat(type.Interfaces)
            .Where(e => model.Type(e) is { } related && SchemaRenderer.IsListed(related, shape) &&
                        related.Kind != SchemaTypeKind.Scalar);

    private IEnumerable<string> ListedTypeNames() =>
        model.Types.Values
            .Where(e => e.Name != model.QueryType && SchemaRenderer.IsListed(e, shape))
            .OrderBy(e => SchemaRenderer.KindOrder(e.Kind))
            .Select(e => e.Name);

    private static Dictionary<string, object> Variables(JsonElement? variables) =>
        variables is { ValueKind: JsonValueKind.Object } values
            ? values.EnumerateObject().ToDictionary(e => e.Name, e => (object)e.Value.Clone())
            : null;

    private static string Summary(string description, bool full = false)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return "";
        }

        return full
            ? "  – " + description.Trim().ReplaceLineEndings(" ")
            : "  – " + SchemaRenderer.FirstLine(description);
    }

    private static string Finish(StringBuilder text) => text.ToString().ReplaceLineEndings("\n").TrimEnd() + "\n";
}
