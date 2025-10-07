namespace Linq2GraphQL.Client;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public class GraphQLMemberAttribute : Attribute
{
    public GraphQLMemberAttribute(string graphQLName)
    {
        GraphQLName = graphQLName;
    }

    public string GraphQLName { get; private set; }
}