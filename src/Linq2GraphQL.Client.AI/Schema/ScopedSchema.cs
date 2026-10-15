using System.Reflection;

namespace Linq2GraphQL.Client.AI.Schema;

/// <summary>
/// The schema as the agent sees it: only the allowed queries, the types reachable from them, no omitted or
/// deprecated members, and the configured descriptions applied. The renderer, the validator and the tools all
/// work from this, never from the full schema. Mutations and subscriptions are left out in this version.
/// </summary>
internal sealed class ScopedSchema
{
    private ScopedSchema(SchemaModel schema, IReadOnlyList<string> instructions)
    {
        Schema = schema;
        Instructions = instructions;
    }

    public SchemaModel Schema { get; }

    public SchemaType QueryType => Schema.Type(Schema.QueryType);

    public IReadOnlyList<string> Instructions { get; }

    public static ScopedSchema Create<TClient>(string sdl, Action<AgentOptions<TClient>> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sdl);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AgentOptions<TClient>();
        configure(options);
        return Create(SdlReader.Read(sdl), options);
    }

    public static ScopedSchema Create<TClient>(SchemaModel full, AgentOptions<TClient> options)
    {
        var resolver = new Resolver(full);

        var allowedRoots = AllowedRoots(full, options, resolver);
        var omitted = options.Omitted
            .Select(e => resolver.Field(e))
            .Where(e => e != null)
            .Select(e => e!.Value)
            .ToHashSet();
        var descriptions = Descriptions(full, options, resolver, omitted);

        if (allowedRoots.Count == 0 && resolver.Errors.Count == 0)
        {
            resolver.Errors.Add("No queries are allowed. Use AllowQuery, or AllowAllQueries for every query.");
        }

        if (resolver.Errors.Count > 0)
        {
            throw new AgentConfigurationException(
                "The agent configuration does not match the schema:" + Environment.NewLine +
                string.Join(Environment.NewLine, resolver.Errors.Select(e => "- " + e)));
        }

        var reachable = Reachable(full, allowedRoots, omitted);
        var types = new Dictionary<string, SchemaType>();
        foreach (var type in full.Types.Values.Where(e => reachable.Contains(e.Name)))
        {
            types[type.Name] = Scope(type, full, allowedRoots, omitted, reachable, descriptions);
        }

        return new ScopedSchema(new SchemaModel { Types = types, QueryType = full.QueryType },
            options.InstructionTexts.ToList());
    }

    private static List<string> AllowedRoots<TClient>(SchemaModel full, AgentOptions<TClient> options,
        Resolver resolver)
    {
        var allowed = new HashSet<string>();

        if (options.AllowAll != null)
        {
            var excluded = options.AllowAll.Excluded.Select(resolver.Query).Where(e => e != null).ToHashSet();
            allowed.UnionWith(full.Type(full.QueryType).Fields.Select(e => e.Name).Where(e => !excluded.Contains(e)));
        }

        foreach (var operation in options.AllowedQueries)
        {
            if (resolver.Query(operation.Method) is { } name)
            {
                allowed.Add(name);
            }
        }

        // Schema order, not configuration order, so the agent always sees the same layout.
        return full.Type(full.QueryType).Fields.Select(e => e.Name).Where(allowed.Contains).ToList();
    }

    private static Dictionary<(string Type, string Member, string Argument), (string Text, bool Append)> Descriptions<TClient>(
        SchemaModel full, AgentOptions<TClient> options, Resolver resolver, HashSet<(string Type, string Field)> omitted)
    {
        var descriptions = new Dictionary<(string, string, string), (string, bool)>();

        foreach (var operation in options.AllowedQueries)
        {
            if (resolver.Query(operation.Method) is not { } name)
            {
                continue;
            }

            if (operation.Description is { } description)
            {
                descriptions[(full.QueryType, name, null)] = description;
            }

            var field = full.Type(full.QueryType).Field(name);
            foreach (var (argument, text, append) in operation.ArgumentDescriptions)
            {
                if (field.Arguments.All(e => e.Name != argument))
                {
                    resolver.Errors.Add($"Query {name} has no argument {argument}. " +
                                        $"Arguments: {Names(field.Arguments.Select(e => e.Name))}.");
                    continue;
                }

                descriptions[(full.QueryType, name, argument)] = (text, append);
            }
        }

        foreach (var (target, text, append) in options.Descriptions)
        {
            if (target.Member != null)
            {
                if (resolver.Field(target) is not { } field)
                {
                    continue;
                }

                if (omitted.Contains(field))
                {
                    resolver.Errors.Add($"{target} is described but also omitted; the agent never sees it.");
                    continue;
                }

                if (full.Type(field.Type).Field(field.Field).IsDeprecated)
                {
                    resolver.Errors.Add($"{target} is described but deprecated, so it is not in the agent's scope.");
                    continue;
                }

                descriptions[(field.Type, field.Field, null)] = (text, append);
            }
            else if (target.EnumValue != null)
            {
                if (resolver.EnumValue(target) is not { } value)
                {
                    continue;
                }

                descriptions[(value.Type, value.Value, null)] = (text, append);
            }
            else if (resolver.Type(target.ClrType) is { } type)
            {
                descriptions[(type.Name, null, null)] = (text, append);
            }
        }

        return descriptions;
    }

    /// <summary>
    /// Every type the agent can reach from the allowed queries, following field types, argument types,
    /// input fields, union members and interface implementations.
    /// </summary>
    private static HashSet<string> Reachable(SchemaModel full, List<string> allowedRoots,
        HashSet<(string Type, string Field)> omitted)
    {
        var reachable = new HashSet<string> { full.QueryType };
        var pending = new Queue<string>();

        void Visit(string name)
        {
            if (reachable.Add(name))
            {
                pending.Enqueue(name);
            }
        }

        foreach (var root in allowedRoots.Select(e => full.Type(full.QueryType).Field(e)))
        {
            Visit(root.Type.NamedType);
            root.Arguments.ForEach(e => Visit(e.Type.NamedType));
        }

        while (pending.TryDequeue(out var name))
        {
            var type = full.Type(name);
            foreach (var field in type.Fields.Where(e => !e.IsDeprecated && !omitted.Contains((name, e.Name))))
            {
                Visit(field.Type.NamedType);
                field.Arguments.ForEach(e => Visit(e.Type.NamedType));
            }

            type.PossibleTypes.ForEach(Visit);
        }

        return reachable;
    }

    private static SchemaType Scope(SchemaType type, SchemaModel full, List<string> allowedRoots,
        HashSet<(string Type, string Field)> omitted, HashSet<string> reachable,
        Dictionary<(string Type, string Member, string Argument), (string Text, bool Append)> descriptions)
    {
        var isQuery = type.Name == full.QueryType;
        var fields = type.Fields
            .Where(e => isQuery ? allowedRoots.Contains(e.Name) : !e.IsDeprecated && !omitted.Contains((type.Name, e.Name)))
            .Select(e =>
            {
                var field = e.Copy();
                field.Description = Apply(field.Description, descriptions, (type.Name, e.Name, null));
                foreach (var argument in field.Arguments)
                {
                    argument.Description = Apply(argument.Description, descriptions, (type.Name, e.Name, argument.Name));
                }

                return field;
            })
            .ToList();

        return new SchemaType
        {
            Name = type.Name,
            Kind = type.Kind,
            Description = Apply(type.Description, descriptions, (type.Name, null, null)),
            Fields = fields,
            EnumValues = type.EnumValues.Where(e => !e.IsDeprecated).Select(e => new SchemaEnumValue
            {
                Name = e.Name,
                Description = Apply(e.Description, descriptions, (type.Name, e.Name, null))
            }).ToList(),
            Interfaces = type.Interfaces.Where(reachable.Contains).ToList(),
            PossibleTypes = type.PossibleTypes.Where(reachable.Contains).ToList()
        };
    }

    private static string Apply(string original,
        Dictionary<(string Type, string Member, string Argument), (string Text, bool Append)> descriptions,
        (string, string, string) key)
    {
        if (!descriptions.TryGetValue(key, out var description))
        {
            return original;
        }

        return description.Append && !string.IsNullOrWhiteSpace(original)
            ? original + "\n" + description.Text
            : description.Text;
    }

    private static string Names(IEnumerable<string> names)
    {
        var list = names.ToList();
        return list.Count == 0 ? "none" : string.Join(", ", list);
    }

    /// <summary>
    /// Maps generated CLR members to schema names. Generated types keep their GraphQL names, properties carry
    /// <see cref="GraphQLMemberAttribute"/>, and other members follow the generator's rule of upper-casing the
    /// first letter. Problems are collected rather than thrown, so one exception can list them all.
    /// </summary>
    private sealed class Resolver(SchemaModel full)
    {
        public List<string> Errors { get; } = [];

        public string Query(MethodInfo method)
        {
            var name = Match(full.Type(full.QueryType).Fields.Select(e => e.Name), method.Name);
            if (name != null)
            {
                return name;
            }

            if (full.MutationType != null && Match(full.Type(full.MutationType).Fields.Select(e => e.Name), method.Name) != null)
            {
                Errors.Add($"{method.Name} is a mutation. Only queries can be allowed in this version.");
            }
            else
            {
                Errors.Add($"The schema has no query {method.Name}.");
            }

            return null;
        }

        public SchemaType Type(Type clrType)
        {
            var type = full.Type(clrType.Name);
            if (type == null)
            {
                Errors.Add($"The schema has no type {clrType.Name}.");
            }

            return type;
        }

        public (string Type, string Field)? Field(MemberReference reference)
        {
            if (Type(reference.ClrType) is not { } type)
            {
                return null;
            }

            var name = reference.Member.GetCustomAttribute<GraphQLMemberAttribute>()?.GraphQLName
                       ?? Match(type.Fields.Select(e => e.Name), reference.Member.Name);
            if (name == null || type.Field(name) == null)
            {
                Errors.Add($"Type {type.Name} has no field {name ?? reference.Member.Name}.");
                return null;
            }

            return (type.Name, name);
        }

        public (string Type, string Value)? EnumValue(MemberReference reference)
        {
            if (Type(reference.ClrType) is not { } type)
            {
                return null;
            }

            if (type.EnumValues.All(e => e.Name != reference.EnumValue))
            {
                Errors.Add($"Enum {type.Name} has no value {reference.EnumValue}.");
                return null;
            }

            if (type.EnumValues.First(e => e.Name == reference.EnumValue).IsDeprecated)
            {
                Errors.Add($"{type.Name}.{reference.EnumValue} is described but deprecated, so it is not in the agent's scope.");
                return null;
            }

            return (type.Name, reference.EnumValue);
        }

        private static string Match(IEnumerable<string> schemaNames, string clrName) =>
            schemaNames.FirstOrDefault(e => ToPascalCase(e) == clrName);

        private static string ToPascalCase(string name) =>
            char.IsUpper(name[0]) ? name : char.ToUpperInvariant(name[0]) + name[1..];
    }
}
