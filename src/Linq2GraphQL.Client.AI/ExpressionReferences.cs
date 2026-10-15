using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Linq2GraphQL.Client.AI;

/// <summary>
/// Turns the typed expressions of the agent configuration into references to generated members.
/// </summary>
internal static class ExpressionReferences
{
    /// <summary>
    /// The operation method behind <c>c =&gt; c.Query.Customers</c>. A method group in an expression tree
    /// becomes a call to <see cref="MethodInfo.CreateDelegate(Type, object)"/> on a constant MethodInfo.
    /// </summary>
    public static MethodInfo Method<TClient>(Expression<Func<TClient, Delegate>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var body = operation.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert
            ? convert.Operand
            : operation.Body;

        if (body is MethodCallExpression { Method.Name: nameof(MethodInfo.CreateDelegate) } call)
        {
            var method = (call.Object as ConstantExpression ?? call.Arguments.OfType<ConstantExpression>().LastOrDefault())
                ?.Value as MethodInfo;
            if (method != null)
            {
                return method;
            }
        }

        throw new ArgumentException(
            $"Expected an operation such as c => c.Query.Customers, but got {operation}.", nameof(operation));
    }

    /// <summary>
    /// The property behind <c>c =&gt; c.Status</c>.
    /// </summary>
    public static MemberReference Member<T>(Expression<Func<T, object>> field)
    {
        ArgumentNullException.ThrowIfNull(field);

        var body = field.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert
            ? convert.Operand
            : field.Body;

        if (body is MemberExpression { Member: PropertyInfo property } member && member.Expression is ParameterExpression)
        {
            return new MemberReference(typeof(T), property, null);
        }

        throw new ArgumentException(
            $"Expected a field of {typeof(T).Name} such as c => c.Name, but got {field}.", nameof(field));
    }

    /// <summary>
    /// The GraphQL name of a generated enum value, taken from its <see cref="EnumMemberAttribute"/>.
    /// </summary>
    public static MemberReference EnumValue<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var name = value.ToString();
        var member = typeof(TEnum).GetField(name);
        var graphQLName = member?.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? name;
        return new MemberReference(typeof(TEnum), null, graphQLName);
    }
}
