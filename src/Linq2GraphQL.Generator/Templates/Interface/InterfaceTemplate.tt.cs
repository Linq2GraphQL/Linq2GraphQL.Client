namespace Linq2GraphQL.Generator.Templates.Interface;

public partial class InterfaceTemplate
{
    private readonly GraphqlType classType;
    private readonly string namespaceName;

    public InterfaceTemplate(GraphqlType classType, string namespaceName)
    {
        this.classType = classType;
        this.namespaceName = namespaceName;
    }

     

    public string GetInterfaceConcreteName()
    {
        return classType.Name + "__Concrete";
    }
   
}