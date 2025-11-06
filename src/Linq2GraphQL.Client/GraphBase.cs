using System.Linq.Expressions;

namespace Linq2GraphQL.Client;

public abstract class GraphBase<T, TGraph>
{
    protected readonly GraphClient client;
    protected readonly OperationType operationType;

    public GraphBase(GraphClient client, string name, OperationType operationType, List<ArgumentValue> arguments)
    {
        this.client = client;
        this.operationType = operationType;
        QueryNode = new(typeof(T), name, arguments, null, true);
    }

    public QueryNode QueryNode { get; }


    /// <summary>
    ///     Include top node
    /// </summary>
    /// <returns></returns>
    public TGraph Include()
    {
        QueryNode.IncludePrimitive = true;
        return (TGraph)(object)this;
    }

    public TGraph Include<TProperty>(Expression<Func<T, TProperty>> path)
    {
        Utilities.ParseExpression(path, QueryNode);
        return (TGraph)(object)this;
    }

    public void ParseExpression(Expression body)
    {
        Utilities.ParseExpression(body, QueryNode);
    }
}