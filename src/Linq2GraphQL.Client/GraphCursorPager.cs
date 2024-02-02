using Linq2GraphQL.Client.Common;
using System.Linq.Expressions;

namespace Linq2GraphQL.Client;

public class GraphCursorPager<T, TResult> where T : ICursorPaging
{
    private GraphCursorQueryExecute<T, TResult> query;

    public GraphCursorPager(GraphCursorQueryExecute<T, TResult> graphCursorQueryExecute)
    {
        this.query = graphCursorQueryExecute;
        IncludeT(e => e.PageInfo);
    }

    public T PagerResult => query.BaseResult;

    private GraphCursorPager<T, TResult> IncludeT<TProperty>(Expression<Func<T, TProperty>> path)
    {
        Utilities.ParseExpression(path, query.QueryNode);
        return this;
    }

    private async Task<TResult> ExecutePagerAsync(CancellationToken cancellationToken = default)
    {
        var baseType = await query.ExecuteBaseAsync(cancellationToken);
        return query.ConvertResult(baseType);
    }

    public async Task<TResult> NextPageAsync(CancellationToken cancellationToken = default)
    {
        query.QueryNode.SetArgumentValue("after", query.BaseResult?.PageInfo?.EndCursor);
        query.QueryNode.SetArgumentValue("before", null);
        return await ExecutePagerAsync(cancellationToken);
    }

    public async Task<TResult> PreviousPageAsync(CancellationToken cancellationToken = default)
    {
        query.QueryNode.SetArgumentValue("after", null);
        query.QueryNode.SetArgumentValue("before", query.BaseResult?.PageInfo?.EndCursor);
        return await ExecutePagerAsync(cancellationToken);
    }
}
