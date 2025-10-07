using System.Linq.Expressions;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.Client;

public class GraphCursorQuery<T> : GraphBase<T, GraphCursorQuery<T>> where T : ICursorPaging
{
    public GraphCursorQuery(GraphClient client, string name, OperationType operationType, List<ArgumentValue> arguments)
        : base(client, name, operationType, arguments)
    {
    }

    public GraphCursorQueryExecute<T, T> Select()
    {
        QueryNode.IncludePrimitiveIfNoChildPrimitive();
        return new(client, operationType, QueryNode, null);
    }

    public GraphCursorQueryExecute<T, TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
    {
        ParseExpression(selector);
        return new(client, operationType, QueryNode, selector);
    }
}