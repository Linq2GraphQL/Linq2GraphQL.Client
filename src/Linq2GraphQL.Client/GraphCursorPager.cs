using Linq2GraphQL.Client.Common;
using System.Linq.Expressions;

namespace Linq2GraphQL.Client;

public class GraphCursorPager<T, TResult> where T : ICursorPaging
{
    private GraphCursorQueryExecute<T, TResult> query;

    // The page size the query was built with. Moving forward pages with `first`, moving
    // back with `last`; a server only counts back from `before` when `last` is set.
    private readonly object pageSize;

    public GraphCursorPager(GraphCursorQueryExecute<T, TResult> graphCursorQueryExecute)
    {
        this.query = graphCursorQueryExecute;
        pageSize = GetArgumentValue("first") ?? GetArgumentValue("last");
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

    private async Task<GraphResult<TResult>> ExecutePagerWithResultAsync(CancellationToken cancellationToken = default)
    {
        var result = await query.ExecuteBaseWithResultAsync(cancellationToken);
        return query.ConvertResultFull(result);
    }

    private object GetArgumentValue(string graphName)
    {
        return query.QueryNode.Arguments.FirstOrDefault(e => e.GraphName == graphName)?.Value;
    }

    private void SetNextPageArguments()
    {
        query.QueryNode.SetArgumentValue("first", pageSize);
        query.QueryNode.SetArgumentValue("after", query.BaseResult?.PageInfo?.EndCursor);
        query.QueryNode.SetArgumentValue("last", null);
        query.QueryNode.SetArgumentValue("before", null);
    }

    private void SetPreviousPageArguments()
    {
        query.QueryNode.SetArgumentValue("first", null);
        query.QueryNode.SetArgumentValue("after", null);
        query.QueryNode.SetArgumentValue("last", pageSize);
        query.QueryNode.SetArgumentValue("before", query.BaseResult?.PageInfo?.StartCursor);
    }

    public async Task<TResult> NextPageAsync(CancellationToken cancellationToken = default)
    {
        SetNextPageArguments();
        return await ExecutePagerAsync(cancellationToken);
    }

    public async Task<GraphResult<TResult>> NextPageWithResultAsync(CancellationToken cancellationToken = default)
    {
        SetNextPageArguments();
        return await ExecutePagerWithResultAsync(cancellationToken);
    }

    public async Task<TResult> PreviousPageAsync(CancellationToken cancellationToken = default)
    {
        SetPreviousPageArguments();
        return await ExecutePagerAsync(cancellationToken);
    }

    public async Task<GraphResult<TResult>> PreviousPageWithResultAsync(CancellationToken cancellationToken = default)
    {
        SetPreviousPageArguments();
        return await ExecutePagerWithResultAsync(cancellationToken);
    }
}
