using System.Linq.Expressions;

namespace Linq2GraphQL.Client;

public abstract class GraphBaseExecute<T, TResult>
{
    protected readonly GraphClient client;
    protected readonly OperationType operationType;
    protected readonly QueryExecutor<T> queryExecutor;
    public readonly QueryNode QueryNode;
    protected readonly Func<T, TResult> mapper;
    protected readonly Expression<Func<T, TResult>> selector;

    public GraphBaseExecute(GraphClient client, OperationType operationType, QueryNode queryNode,
        Expression<Func<T, TResult>> selector)
    {
        this.client = client;
        this.operationType = operationType;
        this.QueryNode = queryNode;
        this.mapper = selector?.Compile();
        this.selector = selector;
        queryExecutor = new QueryExecutor<T>(client);
    }

    public async Task InitQueryAsync()
    {
        var schema = await client.GetSchemaForSafeModeAsync();
        QueryNode.AddPrimitiveChildren(true, schema);
    }

    private string GetQueryArgumentString()
    {
        var args = QueryNode.GetAllActiveArguments();

        if (!args.Any())
        {
            return "";
        }

        var text = "";
        foreach (var argument in args)
        {
            text += $"{argument.VariableName}: {argument.GraphType} ";
        }

        return $"({text})";
    }

    public string GetGraphQLQuery()
    {
        return GetOperationType() +  GetQueryArgumentString() + "{ " + Environment.NewLine +
               QueryNode.GetQueryString() + Environment.NewLine + "}";
    }

    protected string GetOperationType()
    {
        return operationType.ToString().ToLower();
    }

    public TResult ConvertResult(T result)
    {
        if (Equals(result, default(T)))
        {
            return default;
        }

        if (mapper == null)
        {
            return (TResult)(object)result;
        }
        else
        {
            return mapper.Invoke(result);
        }
    }
}
