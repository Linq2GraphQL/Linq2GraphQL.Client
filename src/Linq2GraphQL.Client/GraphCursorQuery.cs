using Linq2GraphQL.Client.Common;
using System.Linq.Expressions;

namespace Linq2GraphQL.Client;

public class GraphCursorQuery<T> : GraphBase<T, GraphCursorQuery<T>> where T : ICursorPaging
{
    public GraphCursorQuery(GraphClient client, string alias, OperationType operationType, List<ArgumentValue> arguments) : base(client, alias, operationType, arguments)
    {
    }

    public GraphCursorQueryExecute<T, T> Select()
    {
        QueryNode.IncludePrimitiveIfNoChildPrimitive();
        return new GraphCursorQueryExecute<T, T>(client, operationType, QueryNode, null);
    }

    public GraphCursorQueryExecute<T, TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
    {
        ParseExpression(selector);
        return new GraphCursorQueryExecute<T, TResult>(client, operationType, QueryNode, selector);
    }
}
