using System.Linq.Expressions;

namespace Linq2GraphQL.Client.Subscriptions;

public class GraphSubscription<T> : GraphBase<T, GraphSubscription<T>> 
{
    public GraphSubscription(GraphClient client, string alias, OperationType operationType, List<ArgumentValue> arguments) : base(client, alias, operationType, arguments)
    {
    }

    public GraphSubscriptionExecute<T, T> Select()
    {
        return new GraphSubscriptionExecute<T, T>(client, operationType, QueryNode, null);
    }

    public GraphSubscriptionExecute<T, TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
    {
        ParseExpression(selector);
        return new GraphSubscriptionExecute<T, TResult>(client, operationType, QueryNode, selector);
    }
}