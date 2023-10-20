namespace Linq2GraphQL.Generator.Templates.Client;

public partial class ClientTemplate
{
    private readonly string name;
    private readonly GraphqlType queryType;
    private readonly GraphqlType mutationType;
    private readonly GraphqlType subscriptionType;

    private bool includeQuery => queryType != null;
    private  bool includeMutation => mutationType != null;
    private  bool includeSubscriptions => subscriptionType != null;
    private readonly string namespaceName;

    public ClientTemplate(string namespaceName, string name, GraphqlType queryType, GraphqlType mutationType, GraphqlType subscriptionType)
    {
        this.namespaceName = namespaceName;
        this.name = name;
        this.queryType = queryType;
        this.mutationType = mutationType;
        this.subscriptionType = subscriptionType;
    }

    private string GetMehodName(GraphqlType graphqlType)
    {
        return graphqlType.CSharpName + "Methods";
    }
    //private string clientName => clientName + "Client";
}