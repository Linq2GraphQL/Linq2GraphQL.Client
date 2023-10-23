using Linq2GraphQL.Client.Common;
using System.Linq.Expressions;

namespace Linq2GraphQL.Client;


public class GraphCursorQueryExecute<T, TResult> : GraphQueryExecute<T, TResult> where T : ICursorPaging
{
    public GraphCursorQueryExecute(GraphClient client, OperationType operationType, QueryNode queryNode, Expression<Func<T, TResult>> selector) : base(client, operationType, queryNode, selector)
    { }

    public GraphCursorPager<T, TResult> AsPager()
    {
        return new GraphCursorPager<T, TResult>(this);
    }
}

public class GraphQueryExecute<T, TResult> : GraphBaseExecute<T, TResult>
{
    public GraphQueryExecute(GraphClient client, OperationType operationType, QueryNode queryNode,
        Expression<Func<T, TResult>> selector) : base(client, operationType, queryNode, selector) { }

    public T BaseResult { get; set; }

    public async Task<T> ExecuteBaseAsync()
    {
        BaseResult = await queryExecutor.ExecuteRequestAsync(QueryNode.Alias, await GetRequestAsync());
        return BaseResult;
    }

    public async Task<TResult> ExecuteAsync()
    {
        return ConvertResult(await ExecuteBaseAsync());
    }

}