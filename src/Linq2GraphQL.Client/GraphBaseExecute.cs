using Linq2GraphQL.Client.Schema;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
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
    private volatile bool intialized;
    private GraphQLSchema schema;

    public GraphBaseExecute(GraphClient client, OperationType operationType, QueryNode queryNode,
        Expression<Func<T, TResult>> selector)
    {
        this.client = client;
        this.operationType = operationType;
        this.QueryNode = queryNode;
        // Identity selector (r => r) is the common case — skip the expression compilation;
        // ConvertResult already handles a null mapper as identity.
        this.mapper = selector == null || selector.Body == selector.Parameters[0] ? null : selector.Compile();
        this.selector = selector;
        queryExecutor = new QueryExecutor<T>(client);
    }

    private async Task InitQueryAsync()
    {
        // Fast path: initialization is one-shot; skip the semaphore once done.
        if (intialized)
        {
            return;
        }

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

    private string GetQueryVariablesString(IReadOnlyList<ArgumentValue> args)
    {
        if (args.Count == 0)
        {
            return "";
        }

        var text = new StringBuilder(args.Count * 32);
        foreach (var argument in args)
        {
            text.Append('$').Append(argument.VariableName).Append(": ").Append(argument.GraphType).Append(' ');
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


    /// <summary>
    /// The request this query would send, built without contacting the server: safe mode's schema check is
    /// skipped, so automatically included fields are not validated against the live schema. Use it to show or
    /// log a query; <see cref="GetRequestAsync"/> builds the request the query actually sends.
    /// </summary>
    public GraphQLRequest GetRequest()
    {
        _lock.Wait();
        try
        {
            if (!intialized)
            {
                QueryNode.SetAllUniqueVariableNames();
                QueryNode.AddPrimitiveChildren(true, null);
                intialized = true;
            }
        }
        finally
        {
            _lock.Release();
        }

        var args = QueryNode.GetAllActiveArguments();

        return new GraphQLRequest
        {
            Query = GetGraphQLQuery(args),
            Variables = args.ToDictionary(x => x.VariableName, e => e.Value)
        };
    }

    public async Task<GraphQLRequest> GetRequestAsync()
    {
        await InitQueryAsync();

        // Compute active arguments once for both the query string and the variables.
        var args = QueryNode.GetAllActiveArguments();

        return new GraphQLRequest
        {
            Query = GetGraphQLQuery(args),
            Variables = args.ToDictionary(x => x.VariableName, e => e.Value)
        };
    }

    private string GetGraphQLQuery(IReadOnlyList<ArgumentValue> args)
    {
        return GetOperationType() + GetQueryVariablesString(args) + "{ " + Environment.NewLine +
               QueryNode.GetQueryString() + Environment.NewLine + "}";
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

    public GraphResult<TResult> ConvertResultFull(GraphResult<T> result)
    {
        return new GraphResult<TResult>
        {
            Data = ConvertResult(result.Data),
            Errors = result.Errors,
            Extensions = result.Extensions
        };
    }
}
