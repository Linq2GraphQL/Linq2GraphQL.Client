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


    public string GetFieldCSharpName(Field field, bool addRequired = false)
    {

        if (field.GraphqlType.IsPageInfo())
        {
            return "Linq2GraphQL.Client.Common.PageInfo";
        }

        var result = "";
        if (addRequired && GeneratorSettings.Current.Nullable && field.FieldInfo.IsNoneNull)
        {
            result += "required ";
        }

        result += field.FieldInfo.CSharpTypeNameFull;

        return result;


    }


    public bool NullableEnabled()
    {
        return GeneratorSettings.Current.Nullable;
    }


}