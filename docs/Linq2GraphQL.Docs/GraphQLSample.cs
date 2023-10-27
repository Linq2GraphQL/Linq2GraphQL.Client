using Linq2GraphQL.Client;

namespace Linq2GraphQL.Docs
{
    public class GraphQLSample<T, TResult>
    {

        public GraphQLSample(GraphQueryExecute<T, TResult> executor, string title)
        {
            Executor = executor;
            Title = title;
        }

        public GraphQueryExecute<T, TResult> Executor { get; }
        public string Title { get; }
    }
}
