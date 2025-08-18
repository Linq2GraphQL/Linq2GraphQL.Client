namespace Linq2GraphQL.Generator.Templates.Client;

public partial class ClientTemplate
{
    private readonly string namespaceName;
    private readonly string name;
    private readonly GraphqlType queryType;
    private readonly GraphqlType mutationType;
    private readonly GraphqlType subscriptionType;
    private readonly bool includeDeprecated;

    public ClientTemplate(string namespaceName, string name, GraphqlType queryType, GraphqlType mutationType, GraphqlType subscriptionType, bool includeDeprecated)
    {
        this.namespaceName = namespaceName;
        this.name = name;
        this.queryType = queryType;
        this.mutationType = mutationType;
        this.subscriptionType = subscriptionType;
        this.includeDeprecated = includeDeprecated;
    }

    private bool includeQuery => queryType != null;
    private bool includeMutation => mutationType != null;
    private bool includeSubscriptions => subscriptionType != null;

    private string GetMehodName(GraphqlType graphqlType)
    {
        return graphqlType.CSharpName + "Methods";
    }
}
