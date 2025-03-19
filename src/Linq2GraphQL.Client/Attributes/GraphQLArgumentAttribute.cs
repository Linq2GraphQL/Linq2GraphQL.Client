namespace Linq2GraphQL.Client;

[AttributeUsage(AttributeTargets.Parameter)]
public class GraphQLArgumentAttribute(string graphQLName, string graphQLType) : Attribute
{
    public string GraphQLType { get; private set; } = graphQLType;
    public string GraphQLName { get; private set; } = graphQLName;
}