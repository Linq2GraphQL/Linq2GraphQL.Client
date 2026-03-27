using System.Net;

namespace Linq2GraphQL.Client;

public class GraphQueryRequestException : Exception
{
    public GraphQueryRequestException(string message, HttpStatusCode statusCode, string query, Dictionary<string, object> variables) :
        base(message)
    {
        StatusCode = statusCode;
        GraphQLQuery = query;
        GraphQLVariables = variables;
    }

    public HttpStatusCode StatusCode { get; }
    public string GraphQLQuery { get; }
    public Dictionary<string, object> GraphQLVariables { get; }
}