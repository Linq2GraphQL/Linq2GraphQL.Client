using System.Reflection;
using System.Runtime.CompilerServices;

namespace Linq2GraphQL.Client.Visitors;

internal enum LinqOperatorKind
{
    /// <summary>Not a LINQ operator at all.</summary>
    None,

    /// <summary>
    ///     The operator replaces the element with something else (<c>Select</c>, <c>SelectMany</c>).
    ///     The selection continues at whatever the projection lambda returns.
    /// </summary>
    Projection,

    /// <summary>
    ///     The operator keeps the source element type, or reduces the sequence to one of its elements or to a
    ///     scalar computed from them (<c>Where</c>, <c>OrderBy</c>, <c>First</c>, <c>Count</c>, ...).
    ///     The selection stays on the source node; any lambda is a client side predicate or key selector whose
    ///     members still have to be fetched for it to work.
    /// </summary>
    PassThrough,

    /// <summary>
    ///     A known LINQ operator that cannot be translated, typically because it combines several sequences.
    /// </summary>
    Unsupported
}

internal static class LinqOperator
{
    private static readonly HashSet<string> ProjectionOperators =
    [
        "Select",
        "SelectMany"
    ];

    private static readonly HashSet<string> PassThroughOperators =
    [
        "All",
        "Any",
        "AsEnumerable",
        "AsQueryable",
        "Average",
        "Cast",
        "Chunk",
        "Count",
        "DefaultIfEmpty",
        "Distinct",
        "DistinctBy",
        "ElementAt",
        "ElementAtOrDefault",
        "First",
        "FirstOrDefault",
        "Last",
        "LastOrDefault",
        "LongCount",
        "Max",
        "MaxBy",
        "Min",
        "MinBy",
        "OfType",
        "Order",
        "OrderBy",
        "OrderByDescending",
        "OrderDescending",
        "Reverse",
        "Single",
        "SingleOrDefault",
        "Skip",
        "SkipLast",
        "SkipWhile",
        "Sum",
        "Take",
        "TakeLast",
        "TakeWhile",
        "ThenBy",
        "ThenByDescending",
        "ToArray",
        "ToHashSet",
        "ToList",
        "Where"
    ];

    internal static LinqOperatorKind Classify(MethodInfo method)
    {
        if (method.DeclaringType != typeof(Queryable) && method.DeclaringType != typeof(Enumerable))
        {
            return LinqOperatorKind.None;
        }

        if (Attribute.GetCustomAttribute(method, typeof(ExtensionAttribute)) == null)
        {
            return LinqOperatorKind.None;
        }

        if (ProjectionOperators.Contains(method.Name))
        {
            return LinqOperatorKind.Projection;
        }

        return PassThroughOperators.Contains(method.Name)
            ? LinqOperatorKind.PassThrough
            : LinqOperatorKind.Unsupported;
    }

    internal static bool IsOperator(MethodInfo method)
    {
        return Classify(method) != LinqOperatorKind.None;
    }
}
