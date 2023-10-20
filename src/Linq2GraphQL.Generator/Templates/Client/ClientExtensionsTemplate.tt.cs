namespace Linq2GraphQL.Generator.Templates.Client;

public partial class ClientExtensionsTemplate
{
    private readonly bool includeSubscriptions;
    private readonly string clientName;
    private readonly string namespaceName;

    public ClientExtensionsTemplate(string namespaceName, string name, bool includeSubscriptions)
    {
        this.namespaceName = namespaceName;
        this.clientName = name;
        this.includeSubscriptions = includeSubscriptions;
    }

    //private string clientName => clientName + "Client";
}