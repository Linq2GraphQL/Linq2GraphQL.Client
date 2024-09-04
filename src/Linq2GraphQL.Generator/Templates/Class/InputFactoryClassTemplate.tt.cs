namespace Linq2GraphQL.Generator.Templates.Class;

public partial class InputFactoryClassTemplate
{
    private readonly IList<GraphqlType> inputs;
    private readonly string namespaceName;

    public InputFactoryClassTemplate(IList<GraphqlType> inputs, string namespaceName)
    {
        this.inputs = inputs;
        this.namespaceName = namespaceName;
    }

   

}