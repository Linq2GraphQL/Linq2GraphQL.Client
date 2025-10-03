namespace Linq2GraphQL.Client;

public class GraphQueryRequestException : Exception
{
    public GraphQueryRequestException(string message, string query, Dictionary<string, object> variables) :
        base(message)
    {
        GraphQLQuery = query;
        GraphQLVariables = variables;
    }

    public string GraphQLQuery { get; private set; }
    public Dictionary<string, object> GraphQLVariables { get; private set; }
}