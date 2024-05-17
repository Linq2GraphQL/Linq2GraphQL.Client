namespace Linq2GraphQL.Generator.Templates.Enum;

public partial class EnumTemplate
{
    private readonly GraphqlType enumType;
    private readonly string namespaceName;
    private readonly EnumGeneratorStrategy enumGeneratorStrategy;

    public EnumTemplate(GraphqlType enumType, string namespaceName, EnumGeneratorStrategy enumGeneratorStrategy)
    {
        this.enumType = enumType;
        this.namespaceName = namespaceName;
        this.enumGeneratorStrategy = enumGeneratorStrategy;
    }
}