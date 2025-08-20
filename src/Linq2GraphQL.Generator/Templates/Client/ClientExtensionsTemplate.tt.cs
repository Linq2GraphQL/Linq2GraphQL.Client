namespace Linq2GraphQL.Generator.Templates.Client;

public partial class ClientExtensionsTemplate
{
    private readonly string namespaceName;
    private readonly string clientName;
    private readonly bool includeSubscriptions;

    public ClientExtensionsTemplate(string namespaceName, string clientName, bool includeSubscriptions)
    {
        this.namespaceName = namespaceName;
        this.clientName = clientName;
        this.includeSubscriptions = includeSubscriptions;
    }
}
