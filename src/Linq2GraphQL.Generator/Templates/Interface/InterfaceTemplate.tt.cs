namespace Linq2GraphQL.Generator.Templates.Interface;

public partial class InterfaceTemplate
{
    private readonly GraphqlType classType;
    private readonly string namespaceName;
    private readonly List<string> implementedBy;

    public InterfaceTemplate(GraphqlType classType, string namespaceName, List<string> implementedBy)
    {
        this.classType = classType;
        this.namespaceName = namespaceName;
        this.implementedBy = implementedBy;
    }

    public string GetInterfaceConverterName()
    {
        return classType.Name + "Converter";
    }

    public string GetInterfaceConcreteName()
    {
        return classType.Name + "__Concrete";
    }
   
}