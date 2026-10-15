using System.Linq.Expressions;
using System.Reflection;

namespace Linq2GraphQL.Client.AI;

/// <summary>
/// What the agent may use and what it is told: allowed operations, omitted fields, descriptions and
/// instructions. Everything is referenced through the generated client, so a schema rename breaks the build,
/// and is checked against the schema when the tools are set up.
/// </summary>
public sealed class AgentOptions<TClient>
{
    internal List<OperationOptions> AllowedQueries { get; } = [];
    internal AllowAllQueriesOptions<TClient> AllowAll { get; private set; }
    internal List<MemberReference> Omitted { get; } = [];
    internal List<DescriptionOverride> Descriptions { get; } = [];
    internal List<string> InstructionTexts { get; } = [];

    /// <summary>
    /// Allows the agent to call a query, for example <c>o.AllowQuery(c =&gt; c.Query.Customers)</c>.
    /// </summary>
    public OperationOptions AllowQuery(Expression<Func<TClient, Delegate>> operation)
    {
        var options = new OperationOptions(ExpressionReferences.Method(operation));
        AllowedQueries.Add(options);
        return options;
    }

    /// <summary>
    /// Allows every query in the schema, for the fastest start. Never the default; narrow it with
    /// <see cref="AllowAllQueriesOptions{TClient}.Except"/>. Rendering, validation, omitted fields and
    /// descriptions still apply.
    /// </summary>
    public AllowAllQueriesOptions<TClient> AllowAllQueries()
    {
        AllowAll ??= new AllowAllQueriesOptions<TClient>();
        return AllowAll;
    }

    /// <summary>
    /// Describes a type to the agent, replacing the schema's description unless <paramref name="append"/> is set.
    /// </summary>
    public AgentOptions<TClient> Describe<T>(string description, bool append = false)
    {
        Descriptions.Add(new DescriptionOverride(new MemberReference(typeof(T), null, null), description, append));
        return this;
    }

    /// <summary>
    /// Describes a field or input field to the agent, for example <c>o.Describe&lt;Customer&gt;(c =&gt; c.Status, "...")</c>.
    /// </summary>
    public AgentOptions<TClient> Describe<T>(Expression<Func<T, object>> field, string description, bool append = false)
    {
        Descriptions.Add(new DescriptionOverride(ExpressionReferences.Member(field), description, append));
        return this;
    }

    /// <summary>
    /// Describes an enum value to the agent, for example <c>o.Describe(CustomerStatus.Active, "...")</c>.
    /// </summary>
    public AgentOptions<TClient> Describe<TEnum>(TEnum value, string description, bool append = false)
        where TEnum : struct, Enum
    {
        Descriptions.Add(new DescriptionOverride(ExpressionReferences.EnumValue(value), description, append));
        return this;
    }

    /// <summary>
    /// Removes a field or input field from the agent's scope: it is neither described nor accepted in queries.
    /// Use it for noise and for data the agent shouldn't touch or its token can't read. The server and the
    /// token remain the security boundary for everything else.
    /// </summary>
    public AgentOptions<TClient> Omit<T>(Expression<Func<T, object>> field)
    {
        Omitted.Add(ExpressionReferences.Member(field));
        return this;
    }

    /// <summary>
    /// General guidance for the agent, for example "All dates are UTC."
    /// </summary>
    public AgentOptions<TClient> Instructions(string instructions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instructions);
        InstructionTexts.Add(instructions);
        return this;
    }
}

/// <summary>
/// Settings for one allowed operation.
/// </summary>
public sealed class OperationOptions
{
    internal OperationOptions(MethodInfo method)
    {
        Method = method;
    }

    internal MethodInfo Method { get; }
    internal (string Text, bool Append)? Description { get; private set; }
    internal List<(string Argument, string Text, bool Append)> ArgumentDescriptions { get; } = [];

    /// <summary>
    /// Describes the operation to the agent, replacing the schema's description unless <paramref name="append"/> is set.
    /// </summary>
    public OperationOptions Describe(string description, bool append = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        Description = (description, append);
        return this;
    }

    /// <summary>
    /// Describes one of the operation's arguments by its GraphQL name, for example <c>.Argument("first", "Max 50.")</c>.
    /// </summary>
    public OperationOptions Argument(string name, string description, bool append = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentDescriptions.Add((name, description, append));
        return this;
    }
}

/// <summary>
/// Settings for <see cref="AgentOptions{TClient}.AllowAllQueries"/>.
/// </summary>
public sealed class AllowAllQueriesOptions<TClient>
{
    internal List<MethodInfo> Excluded { get; } = [];

    /// <summary>
    /// Keeps these queries out of the allow-list, for example <c>.Except(c =&gt; c.Query.AuditLog)</c>.
    /// </summary>
    public AllowAllQueriesOptions<TClient> Except(params Expression<Func<TClient, Delegate>>[] operations)
    {
        Excluded.AddRange(operations.Select(ExpressionReferences.Method));
        return this;
    }
}

/// <summary>
/// A reference to a generated type, or one of its members or enum values, resolved against the schema later.
/// </summary>
internal sealed record MemberReference(Type ClrType, MemberInfo Member, string EnumValue)
{
    public override string ToString() => Member != null ? $"{ClrType.Name}.{Member.Name}"
        : EnumValue != null ? $"{ClrType.Name}.{EnumValue}"
        : ClrType.Name;
}

internal sealed record DescriptionOverride(MemberReference Target, string Text, bool Append);
