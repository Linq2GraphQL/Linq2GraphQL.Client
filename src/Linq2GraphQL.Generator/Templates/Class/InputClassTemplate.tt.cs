namespace Linq2GraphQL.Generator.Templates.Class;

public partial class InputClassTemplate
{
    private readonly GraphqlType classType;
    private readonly string namespaceName;

    public InputClassTemplate(GraphqlType classType, string namespaceName)
    {
        this.classType = classType;
        this.namespaceName = namespaceName;
    }

    public bool IsInput => classType.Kind == TypeKind.InputObject;
}