namespace Linq2GraphQL.Client;

public class GraphResult<T>
{
    public T Data { get; init; }
    public List<GraphQueryError> Errors { get; init; }
    public Dictionary<string, object> Extensions { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };
    public bool HasData => Data is not null;

    public void EnsureNoErrors()
    {
        if (HasErrors)
        {
            throw new GraphQueryExecutionException(Errors, string.Empty, null);
        }
    }
}