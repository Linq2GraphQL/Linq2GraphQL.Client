namespace Linq2GraphQL.Generator.Templates.Client;

public partial class IClientTemplate(string namespaceName, string name, GraphqlType queryType, GraphqlType mutationType, GraphqlType subscriptionType, bool includeDeprecated)
{
	private bool includeQuery => queryType != null;
	private bool includeMutation => mutationType != null;
	private bool includeSubscriptions => subscriptionType != null;

	private string GetMehodName(GraphqlType graphqlType)
	{
		return graphqlType.CSharpName + "Methods";
	}
}
