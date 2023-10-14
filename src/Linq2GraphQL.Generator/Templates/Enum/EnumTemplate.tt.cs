namespace Linq2GraphQL.Generator.Templates.Enum;

public partial class EnumTemplate
{
    private readonly GraphqlType enumType;
    private readonly string namespaceName;

    public EnumTemplate(GraphqlType enumType, string namespaceName)
    {
        this.enumType = enumType;
        this.namespaceName = namespaceName;
    }
}