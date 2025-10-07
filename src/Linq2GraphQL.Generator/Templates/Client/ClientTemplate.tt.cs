namespace Linq2GraphQL.Generator.Templates.Client;

public partial class ClientTemplate(
    string namespaceName,
    string name,
    GraphqlType queryType,
    GraphqlType mutationType,
    GraphqlType subscriptionType,
    bool includeDeprecated,
    bool nullable)
{
    private bool includeQuery => queryType != null;
    private bool includeMutation => mutationType != null;
    private bool includeSubscriptions => subscriptionType != null;

    private string GetParameterName(GraphqlType graphqlType)
    {
        var parameterName = GetTypeName(graphqlType);
        if (nullable)
        {
            parameterName += "?";
        }
        
        return parameterName;
    }
    
    private string GetTypeName(GraphqlType graphqlType)
    {
        return graphqlType.CSharpName + "Methods";
    }
}