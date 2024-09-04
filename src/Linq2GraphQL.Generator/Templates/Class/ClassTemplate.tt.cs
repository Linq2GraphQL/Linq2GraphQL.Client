namespace Linq2GraphQL.Generator.Templates.Class;

public partial class ClassTemplate
{
    private readonly GraphqlType classType;
    private readonly string namespaceName;

    public ClassTemplate(GraphqlType classType, string namespaceName)
    {
        this.classType = classType;
        this.namespaceName = namespaceName;
    }


    public bool IsInput => classType.Kind == TypeKind.InputObject;


    public string GetFieldCSharpName(Field field)
    {

        if (field.GraphqlType.IsPageInfo())
        {
            return "Linq2GraphQL.Client.Common.PageInfo";
        }
        return field.FieldInfo.CSharpTypeNameFull;
    }


    public bool NullableEnabled()
    {
        return GeneratorSettings.Current.Nullable;
    }


}