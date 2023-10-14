namespace Linq2GraphQL.Client;

public class GraphQueryRequestException : Exception
{
    public GraphQueryRequestException(string message, string query) : base(message)
    {
        GraphQLQuery = query;
    }

    public string GraphQLQuery { get; private set; }
}