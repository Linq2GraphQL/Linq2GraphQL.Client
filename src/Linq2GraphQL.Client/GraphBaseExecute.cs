using Linq2GraphQL.Client.Schema;
using System.Linq.Expressions;
using System.Text.Json;

namespace Linq2GraphQL.Client;

public abstract class GraphBaseExecute<T, TResult>
{
    protected readonly GraphClient client;
    protected readonly OperationType operationType;
    protected readonly QueryExecutor<T> queryExecutor;
    public readonly QueryNode QueryNode;
    protected readonly Func<T, TResult> mapper;
    protected readonly Expression<Func<T, TResult>> selector;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool intialized;
    private GraphQLSchema schema;

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

    private async Task InitQueryAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!intialized)
            {
                schema = await client.GetSchemaForSafeModeAsync();
                QueryNode.SetAllUniqueVariableNames();
                QueryNode.AddPrimitiveChildren(true, schema);
                intialized = true;
            }

        }
        finally
        {
            _lock.Release();
        }
    }

    public GraphQLSchema Schema => schema;

    private string GetQueryVariablesString()
    {
        var args = QueryNode.GetAllActiveArguments();

        if (!args.Any())
        {
            return "";
        }

        var text = "";
        foreach (var argument in args)
        {
            text += $"${argument.VariableName}: {argument.GraphType} ";
        }

        return $"({text})";
    }

    public async Task<string> GetRequestAsJsonAsync()
    {
        

        var request = await GetRequestAsync();


        var result = request.Query;

        result += Environment.NewLine;
        result += Environment.NewLine;
        result += JsonSerializer.Serialize(request.Variables, client.SerializerOptions);

        return result;
    }


    public async Task<GraphQLRequest> GetRequestAsync()
    {
        await InitQueryAsync();

        return new GraphQLRequest
        {
            Query = GetGraphQLQuery(),
            Variables = QueryNode.GetAllActiveArguments().ToDictionary(x => x.VariableName, e => e.Value)
        };
    }

    private string GetGraphQLQuery()
    {
        var query = GetOperationType() + GetQueryVariablesString() + "{ " + Environment.NewLine +
               QueryNode.GetQueryString() + Environment.NewLine + "}";

        return query;
    }

    private string GetOperationType()
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
