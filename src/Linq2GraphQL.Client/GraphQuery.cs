using System.Linq.Expressions;
using System.Xml.Linq;

namespace Linq2GraphQL.Client;

public class GraphQuery<T> : GraphBase<T, GraphQuery<T>>
{
    public GraphQuery(GraphClient client, string alias, OperationType operationType, List<ArgumentValue> arguments) : base(client, alias, operationType, arguments)
    {
    }

    public GraphQueryExecute<T, T> Select()
    {
        QueryNode.IncludePrimitiveIfNoChildPrimitive();
        return new GraphQueryExecute<T, T>(client, operationType, QueryNode, null);
    }

    public GraphQueryExecute<T, TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
    {
        ParseExpression(selector);
        return new GraphQueryExecute<T, TResult>(client, operationType, QueryNode, selector);
    }
}