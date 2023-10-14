namespace Linq2GraphQL.Generator.Templates.Methods;

public partial class MethodsTemplate
{
    private readonly GraphqlType methodsType;
    private readonly string namespaceName;
    private readonly string operationType;

    public MethodsTemplate(GraphqlType methodsType, string namespaceName, string operationType)
    {
        this.methodsType = methodsType;
        this.namespaceName = namespaceName;
        this.operationType = operationType;

    }

    private string ClassName => methodsType.Name + "Methods";

    private bool isSubscription => operationType == "OperationType.Subscription";
        


    private string GetReturnTypeString(Field field)
    {
        var cSharpTypeNameFull = field.FieldInfo.CSharpTypeNameFull;

        if (cSharpTypeNameFull.Contains("Connection"))
        {
            var kalle = methodsType;
        }

        if (isSubscription)
        {
            return $"GraphSubscription<{cSharpTypeNameFull}>";
        }

        if (field.SupportCursorPaging())
        {
            return $"GraphCursorQuery<{cSharpTypeNameFull}>";
        }

        return $"GraphQuery<{cSharpTypeNameFull}>";
    }

    private string GetReturnBuilderString(Field field)
    {
        return $"new {GetReturnTypeString(field)}";
        //var cSharpTypeNameFull = field.FieldInfo.CSharpTypeNameFull;
        //if (isSubscription)
        //{
        //    return $"new GraphSubscription<{cSharpTypeNameFull}>";
        //}

        //if (field.GraphqlType.SupportCursorPaging())
        //{
        //    return $"new GraphCursorQuery<{cSharpTypeNameFull}>";
        //}

        //return $"new GraphQuery<{cSharpTypeNameFull}>";
    }
}