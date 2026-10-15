using System.Text;

namespace Linq2GraphQL.Client.AI.Schema;

/// <summary>
/// How much of the scoped schema the agent is shown at once.
/// </summary>
internal enum Detail
{
    /// <summary>Allowed queries with one-line descriptions, and type names.</summary>
    Index,

    /// <summary>Adds argument types, every type's fields, and filter and sort summaries.</summary>
    Outline,

    /// <summary>Adds full descriptions, enum value meanings and argument descriptions.</summary>
    Full
}

/// <summary>
/// Turns the scoped schema into the text the agent reads. It stays close to SDL, because models know it and the
/// agent writes GraphQL anyway, and is simplified by collapsing plumbing: connections become
/// <c>[Node] @paged</c> with one shared paging rule, and filter and sort inputs become per-type summaries with
/// one shared syntax rule. Output is deterministic, so it can be snapshot-tested.
/// </summary>
internal static class SchemaRenderer
{
    private const string Indent = "  ";

    /// <summary>
    /// The operator sets HotChocolate generates, named once in the conventions so each filterable field needs
    /// only the set's name. Any other set is listed in full where it is used.
    /// </summary>
    private static readonly (string Name, string[] Operators)[] OperatorSets =
    [
        ("text", ["eq", "neq", "contains", "ncontains", "in", "nin", "startsWith", "nstartsWith", "endsWith", "nendsWith"]),
        ("comparable", ["eq", "neq", "in", "nin", "gt", "ngt", "gte", "ngte", "lt", "nlt", "lte", "nlte"]),
        ("equality", ["eq", "neq", "in", "nin"])
    ];

    public static string Render(ScopedSchema schema, Detail detail)
    {
        var shape = new SchemaShape(schema.Schema);
        var text = new StringBuilder();

        RenderInstructions(text, schema);
        RenderConventions(text, shape, detail == Detail.Index ? [] : UsedOperatorSets(schema.Schema, shape));

        text.AppendLine("Queries:");
        foreach (var root in schema.QueryType.Fields)
        {
            RenderRoot(text, root, shape, detail);
        }

        if (schema.Examples.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Examples (describe_operation shows their queries):");
            foreach (var example in schema.Examples)
            {
                text.Append(Indent).AppendLine($"{example.Name}  – {example.Description}");
            }
        }

        var types = schema.Schema.Types.Values
            .Where(e => e.Name != schema.Schema.QueryType && IsListed(e, shape))
            .OrderBy(e => KindOrder(e.Kind))
            .ToList();

        if (detail == Detail.Index)
        {
            if (types.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("Types: " + string.Join(", ", types.Select(e => e.Name)));
            }

            return Finish(text);
        }

        if (types.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Types:");
            foreach (var type in types)
            {
                RenderType(text, type, shape, detail, Indent);
            }
        }

        RenderFilters(text, schema.Schema, shape);
        RenderSorting(text, schema.Schema, shape);

        return Finish(text);
    }

    /// <summary>
    /// The named types at Full detail, for describing them on demand. Plumbing types are explained by the
    /// conventions instead, and unknown names are skipped.
    /// </summary>
    public static string RenderTypes(ScopedSchema schema, IEnumerable<string> typeNames, Detail detail = Detail.Full)
    {
        var shape = new SchemaShape(schema.Schema);
        var text = new StringBuilder();

        foreach (var name in typeNames.Distinct())
        {
            if (schema.Schema.Type(name) is { } type && IsListed(type, shape))
            {
                RenderType(text, type, shape, detail, "");
            }
        }

        return Finish(text);
    }

    internal static bool IsListed(SchemaType type, SchemaShape shape) =>
        !shape.IsPlumbing(type.Name) && !IsBuiltInScalar(type);

    /// <summary>
    /// Types are grouped by kind, each group in schema order: the shapes the agent selects from first,
    /// scalars last.
    /// </summary>
    internal static int KindOrder(SchemaTypeKind kind) => kind switch
    {
        SchemaTypeKind.Interface => 0,
        SchemaTypeKind.Object => 1,
        SchemaTypeKind.Union => 2,
        SchemaTypeKind.Enum => 3,
        SchemaTypeKind.InputObject => 4,
        _ => 5
    };

    private static List<(string Name, string[] Operators)> UsedOperatorSets(SchemaModel schema, SchemaShape shape)
    {
        var used = schema.Types.Values
            .Where(e => shape.IsOperatorFilter(e) || shape.IsListFilter(e))
            .Select(e => shape.IsListFilter(e) ? ListElement(schema, e) : e)
            .Where(e => e != null && shape.IsOperatorFilter(e))
            .Select(e => OperatorSetName(Operators(e)))
            .ToHashSet();

        return OperatorSets.Where(e => used.Contains(e.Name)).ToList();
    }

    private static string[] Operators(SchemaType operatorFilter) =>
        operatorFilter.Fields.Where(e => e.Name is not ("and" or "or")).Select(e => e.Name).ToArray();

    private static string OperatorSetName(string[] operators) =>
        OperatorSets.FirstOrDefault(e => e.Operators.SequenceEqual(operators)).Name;

    private static string OperatorText(SchemaType operatorFilter)
    {
        var operators = Operators(operatorFilter);
        return OperatorSetName(operators) ?? string.Join(" ", operators);
    }

    /// <summary>
    /// The filter a list filter applies to each element, through <c>some</c>, <c>all</c> or <c>none</c>.
    /// </summary>
    private static SchemaType ListElement(SchemaModel schema, SchemaType listFilter) =>
        listFilter.Fields.FirstOrDefault(e => e.Name != "any") is { } element ? schema.Type(element.Type.NamedType) : null;

    private static bool IsBuiltInScalar(SchemaType type) =>
        type.Kind == SchemaTypeKind.Scalar && type.Name is "String" or "Int" or "Float" or "Boolean" or "ID";

    private static void RenderInstructions(StringBuilder text, ScopedSchema schema)
    {
        if (schema.Instructions.Count == 0)
        {
            return;
        }

        text.AppendLine("Instructions:");
        foreach (var instruction in schema.Instructions)
        {
            text.AppendLine("- " + instruction);
        }

        text.AppendLine();
    }

    private static void RenderConventions(StringBuilder text, SchemaShape shape,
        List<(string Name, string[] Operators)> operatorSets)
    {
        if (!shape.HasConnections && !shape.HasFilters && !shape.HasSorting)
        {
            return;
        }

        text.AppendLine("Conventions:");
        if (shape.HasConnections)
        {
            text.AppendLine("- @paged fields return a page: select `nodes { ... }` and `pageInfo { hasNextPage endCursor }`, " +
                            "and pass `after: <endCursor>` for the next page. @paged(totalCount) also has `totalCount`. " +
                            "@paged(edges) has no `nodes`: select `edges { node { ... } }`.");
        }

        if (shape.HasFilters)
        {
            text.AppendLine("- `where` filters: `where: { field: { operator: value } }`, combined with " +
                            "`and: [ ... ]` and `or: [ ... ]`. A field of a related type nests: `{ customer: { status: { eq: ACTIVE } } }`. " +
                            "List fields take `some`, `all` or `none: { ... }`, or `any: true`.");

            if (operatorSets.Count > 0)
            {
                text.AppendLine("- Operator sets: " +
                                string.Join("; ", operatorSets.Select(e => $"{e.Name} = {string.Join(" ", e.Operators)}")) + ".");
            }
        }

        if (shape.HasSorting)
        {
            text.AppendLine("- `order` sorts: `order: [{ field: ASC }]`, or DESC.");
        }

        text.AppendLine();
    }

    private static void RenderRoot(StringBuilder text, SchemaField root, SchemaShape shape, Detail detail)
    {
        if (detail == Detail.Index)
        {
            var arguments = root.Arguments.Count == 0 ? "" : $"({string.Join(", ", root.Arguments.Select(e => e.Name))})";
            text.Append(Indent).Append(root.Name).Append(arguments).Append(": ").Append(TypeText(root.Type, shape));
            if (FirstLine(root.Description) is { } summary)
            {
                text.Append("  – ").Append(summary);
            }

            text.AppendLine();
            return;
        }

        if (detail == Detail.Full)
        {
            RenderDescription(text, root.Description, Indent);
        }

        RenderField(text, root, shape, detail, Indent);
    }

    private static void RenderType(StringBuilder text, SchemaType type, SchemaShape shape, Detail detail,
        string indent)
    {
        if (detail == Detail.Full)
        {
            RenderDescription(text, type.Description, indent);
        }

        switch (type.Kind)
        {
            case SchemaTypeKind.Scalar:
                text.Append(indent).AppendLine($"scalar {type.Name}");
                return;
            case SchemaTypeKind.Union:
                text.Append(indent).AppendLine($"union {type.Name} = {string.Join(" | ", type.PossibleTypes)}");
                return;
            case SchemaTypeKind.Enum when detail == Detail.Outline:
                text.Append(indent).AppendLine($"enum {type.Name} {{ {string.Join(" ", type.EnumValues.Select(e => e.Name))} }}");
                return;
            case SchemaTypeKind.Enum:
                text.Append(indent).AppendLine($"enum {type.Name} {{");
                foreach (var value in type.EnumValues)
                {
                    RenderDescription(text, value.Description, indent + Indent);
                    text.Append(indent).Append(Indent).AppendLine(value.Name);
                }

                text.Append(indent).AppendLine("}");
                return;
        }

        var keyword = type.Kind switch
        {
            SchemaTypeKind.Interface => "interface",
            SchemaTypeKind.InputObject => "input",
            _ => "type"
        };
        var implements = type.Interfaces.Count == 0 ? "" : " implements " + string.Join(" & ", type.Interfaces);

        if (detail == Detail.Outline)
        {
            var fields = type.Fields.Select(e => FieldText(e, shape, Detail.Outline));
            text.Append(indent).AppendLine($"{keyword} {type.Name}{implements} {{ {string.Join("  ", fields)} }}");
            return;
        }

        text.Append(indent).AppendLine($"{keyword} {type.Name}{implements} {{");
        foreach (var field in type.Fields)
        {
            RenderDescription(text, field.Description, indent + Indent);
            RenderField(text, field, shape, detail, indent + Indent);
        }

        text.Append(indent).AppendLine("}");
    }

    /// <summary>
    /// A field on its own line. At Full detail, arguments with descriptions get a line each, like SDL.
    /// </summary>
    private static void RenderField(StringBuilder text, SchemaField field, SchemaShape shape, Detail detail,
        string indent)
    {
        if (detail != Detail.Full || field.Arguments.All(e => string.IsNullOrWhiteSpace(e.Description)))
        {
            text.Append(indent).AppendLine(FieldText(field, shape, detail));
            return;
        }

        text.Append(indent).Append(field.Name).AppendLine("(");
        foreach (var argument in field.Arguments)
        {
            RenderDescription(text, argument.Description, indent + Indent);
            text.Append(indent).Append(Indent).AppendLine(ArgumentText(argument));
        }

        text.Append(indent).Append("): ").AppendLine(TypeText(field.Type, shape));
    }

    private static string FieldText(SchemaField field, SchemaShape shape, Detail detail)
    {
        var arguments = field.Arguments.Count == 0
            ? ""
            : $"({string.Join(", ", field.Arguments.Select(ArgumentText))})";
        var defaultValue = field.DefaultValue == null || detail == Detail.Index ? "" : $" = {field.DefaultValue}";
        return $"{field.Name}{arguments}: {TypeText(field.Type, shape)}{defaultValue}";
    }

    private static string ArgumentText(SchemaArgument argument) =>
        argument.DefaultValue == null
            ? $"{argument.Name}: {argument.Type}"
            : $"{argument.Name}: {argument.Type} = {argument.DefaultValue}";

    /// <summary>
    /// The type as the agent should read it: a connection becomes its node type marked @paged.
    /// </summary>
    internal static string TypeText(TypeRef type, SchemaShape shape)
    {
        if (shape.ConnectionOf(type.NamedType) is not { } connection)
        {
            return type.ToString();
        }

        var marker = connection.HasNodes switch
        {
            false => "@paged(edges)",
            true when connection.HasTotalCount => "@paged(totalCount)",
            true => "@paged"
        };
        return $"[{connection.NodeType}] {marker}";
    }

    private static void RenderFilters(StringBuilder text, SchemaModel schema, SchemaShape shape)
    {
        var filters = schema.Types.Values.Where(shape.IsEntityFilter).ToList();
        if (filters.Count == 0)
        {
            return;
        }

        text.AppendLine();
        text.AppendLine("Filters:");
        foreach (var filter in filters)
        {
            text.Append(Indent).AppendLine($"{filter.Name}: {FilterSummary(schema, shape, filter)}");
        }
    }

    /// <summary>
    /// One entity filter as a line, for example <c>customerName (text), orders (list of OrderFilterInput: …)</c>.
    /// </summary>
    internal static string FilterSummary(SchemaModel schema, SchemaShape shape, SchemaType filter) =>
        string.Join(", ", filter.Fields
            .Where(e => e.Name is not ("and" or "or"))
            .Select(e => $"{e.Name} {FilterOperatorsOf(schema, shape, e)}"));

    /// <summary>
    /// One sort input as a line, for example <c>orderId, customer (CustomerSortInput)</c>.
    /// </summary>
    internal static string SortSummary(SchemaModel schema, SchemaShape shape, SchemaType sort) =>
        string.Join(", ", sort.Fields.Select(e => shape.IsSortEnum(schema.Type(e.Type.NamedType))
            ? e.Name
            : $"{e.Name} ({e.Type.NamedType})"));

    private static string FilterOperatorsOf(SchemaModel schema, SchemaShape shape, SchemaField field)
    {
        var type = schema.Type(field.Type.NamedType);
        if (shape.IsOperatorFilter(type))
        {
            return $"({OperatorText(type)})";
        }

        if (shape.IsListFilter(type))
        {
            return ListElement(schema, type) switch
            {
                null => "(list: any)",
                var element when shape.IsOperatorFilter(element) => $"(list of {OperatorText(element)}: some/all/none, any)",
                var element => $"(list of {element.Name}: some/all/none, any)"
            };
        }

        return $"({type.Name})";
    }

    private static void RenderSorting(StringBuilder text, SchemaModel schema, SchemaShape shape)
    {
        var sorts = schema.Types.Values.Where(shape.IsSortInput).ToList();
        if (sorts.Count == 0)
        {
            return;
        }

        text.AppendLine();
        text.AppendLine("Sorting:");
        foreach (var sort in sorts)
        {
            text.Append(Indent).AppendLine($"{sort.Name}: {SortSummary(schema, shape, sort)}");
        }
    }

    private static void RenderDescription(StringBuilder text, string description, string indent)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        var escaped = description.Trim().Replace("\\", "\\\\").Replace("\"", "\\\"").ReplaceLineEndings("\\n");
        text.Append(indent).Append('"').Append(escaped).AppendLine("\"");
    }

    internal static string FirstLine(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        var end = trimmed.IndexOfAny(['\r', '\n']);
        return end < 0 ? trimmed : trimmed[..end].TrimEnd();
    }

    private static string Finish(StringBuilder text) => text.ToString().ReplaceLineEndings("\n").TrimEnd() + "\n";
}
