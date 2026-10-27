using System.Runtime.ExceptionServices;

namespace Linq2GraphQL.Client;

public sealed class GraphBatchResult
{
    private readonly IReadOnlyList<GraphQueryExecutionException> executionErrors;

    internal GraphBatchResult(IReadOnlyList<GraphQueryExecutionException> executionErrors,
        IReadOnlyList<Exception> failures)
    {
        this.executionErrors = executionErrors;
        Failures = failures;
        Errors = executionErrors.SelectMany(e => e.Errors).ToList();
    }

    /// <summary>The GraphQL errors of every request in the batch; an error's path starts with its item's alias.</summary>
    public IReadOnlyList<GraphQueryError> Errors { get; }

    /// <summary>Requests that got no GraphQL response, such as HTTP errors and timeouts.</summary>
    public IReadOnlyList<Exception> Failures { get; }

    public bool HasErrors => Errors.Count > 0 || Failures.Count > 0;

    /// <summary>
    /// Throws when the batch had errors: the single exception as is, several as an
    /// <see cref="AggregateException"/>. GraphQL errors are a <see cref="GraphQueryExecutionException"/>
    /// per request.
    /// </summary>
    public void EnsureNoErrors()
    {
        var exceptions = executionErrors.Concat(Failures).ToList();
        if (exceptions.Count == 1)
        {
            ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
        }

        if (exceptions.Count > 1)
        {
            throw new AggregateException("Several requests of the batch failed.", exceptions);
        }
    }
}
