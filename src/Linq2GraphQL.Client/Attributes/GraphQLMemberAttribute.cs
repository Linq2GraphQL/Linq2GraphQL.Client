namespace Linq2GraphQL.Client;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public class GraphQLMemberAttribute : Attribute
{
    public GraphQLMemberAttribute(string graphQLName, bool interfaceProperty = false)
    {
        GraphQLName = graphQLName;
        InterfaceProperty = interfaceProperty;
    }

    public string GraphQLName { get; private set; }

    public bool InterfaceProperty { get; set; }
}