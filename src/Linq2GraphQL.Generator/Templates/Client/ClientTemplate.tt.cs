namespace Linq2GraphQL.Generator.Templates.Client;

public partial class ClientTemplate
{
    private readonly bool includeSubscriptions;
    private readonly string name;
    private readonly bool includeQuery;
    private readonly bool includeMutation;
    private readonly string namespaceName;

    public ClientTemplate(string namespaceName, string name, bool includeQuery, bool includeMutation, bool includeSubscriptions)
    {
        this.namespaceName = namespaceName;
        this.name = name;
        this.includeQuery = includeQuery;
        this.includeMutation = includeMutation;
        this.includeSubscriptions = includeSubscriptions;
    }

    private string clientName => name + "Client";
}