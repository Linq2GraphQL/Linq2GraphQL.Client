using System.Linq.Expressions;

namespace Linq2GraphQL.Client;

/// <summary>
///     Thrown when an expression passed to <c>Include</c> or <c>Select</c> cannot be turned into a GraphQL
///     selection, either because it uses a construct that has no GraphQL equivalent or because it reads
///     something that is not part of the query.
/// </summary>
/// <remarks>
///     The query text does not exist yet when translation fails, so unlike
///     <see cref="GraphQueryRequestException" /> and <see cref="GraphQueryExecutionException" /> this
///     exception carries the offending <see cref="Expression" /> instead.
///     It derives from <see cref="NotSupportedException" />, which is what the parser threw before this type
///     existed, so callers that catch that keep working.
/// </remarks>
public class GraphQueryTranslationException : NotSupportedException
{
    public GraphQueryTranslationException(string message, Expression expression, string memberName = null)
        : base(message)
    {
        Expression = expression;
        MemberName = memberName;
    }

    /// <summary>
    ///     The part of the expression that could not be translated.
    /// </summary>
    public Expression Expression { get; }

    /// <summary>
    ///     The LINQ operator or GraphQL member the failure is about, when it is about a single one.
    /// </summary>
    public string MemberName { get; }
}
