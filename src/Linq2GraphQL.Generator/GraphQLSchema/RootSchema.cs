using Linq2GraphQL.Client.Common;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Text.Json.Serialization;

namespace Linq2GraphQL.Generator;

public class RootSchema
{
    public Data Data { get; set; }
}

public class Data
{
    [JsonPropertyName("__schema")] public Schema Schema { get; set; }
}

public class GraphqlSchemaType
{
    public string Name { get; set; }
}

public class GraphqlType : BaseType
{

    public List<EnumValue> EnumValues { get; set; }
    public List<Field> Fields { get; set; }
    public List<Field> InputFields { get; set; }

    public List<Field> AllFields => Fields ?? InputFields ?? new();

    public List<GraphqlType> Interfaces { get; set; }

    public bool HasInterfaces => (Interfaces != null && Interfaces.Any());

    public string GetInterfacesString(string baseClass = null)
    {
        var interfaces = "";

        if (!string.IsNullOrWhiteSpace(baseClass))
        {
            interfaces += baseClass;
        }

        if (HasInterfaces)
        {
            if (!string.IsNullOrWhiteSpace(interfaces))
            {
                interfaces += ", ";
            }
            interfaces += string.Join(", ", Interfaces.Select(e => e.Name));
        }

        if (ContainPageInfo())
        {
            if (!string.IsNullOrWhiteSpace(interfaces))
            {
                interfaces += ", ";
            }

            interfaces += "Linq2GraphQL.Client.Common.ICursorPaging";
        }

       

        if (!string.IsNullOrWhiteSpace(interfaces))
        {
            interfaces = ": " + interfaces;
        }

        return interfaces;
    }

    public bool IsPageInfo()
    {
        if (CSharpName != "PageInfo") { return false; }
        if (InputFields != null && InputFields.Any()) { return false; }
        //TODO Fix this
        foreach (var field in Fields)
        {

        }
        return true;
    }

    public bool ContainPageInfo()
    {
        return Fields?.Any(e => e.GraphqlType.IsPageInfo()) == true;
    }


}

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum TypeKind
{
    NotSet,
    List,
    [EnumMember(Value = "NON_NULL")] NonNull,
    Scalar,
    Object,
    Interface,
    Union,
    Enum,
    [EnumMember(Value = "INPUT_OBJECT")] InputObject
}

public class EnumValue
{
    public string Name { get; set; }
    public string Description { get; set; }

    public string GetCSharpName()
    {
        var names = Name.ToLower()
            .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var newName = string.Join("", names.Select(e => e.ToPascalCase()));
        return newName;
    }
}


public class BaseField
{

    public string Name { get; set; }
    public string Description { get; set; }

    public string CSharpName => Name?.ToPascalCase();

    public GraphqlType GraphqlType { get; set; }


    public BaseType Type { get; set; }


    private TypeInfo fieldInfo;
    public TypeInfo FieldInfo
    {
        get
        {
            fieldInfo ??= GetFieldTypeInfo();
            return fieldInfo;
        }
    }

    private TypeInfo GetFieldTypeInfo()
    {

        if (Type == null) return null;

        var allTypes = Type.GetAllTypes();

        var baseFieldType = Type.GetBaseBaseType();

        Type csharpType = null;

        string csharpTypeName;
        if (Helpers.TypeMapping.TryGetValue(baseFieldType.Name, out var typeMapping))
        {
            csharpTypeName = typeMapping.Name;
            csharpType = typeMapping.type;
        }
        else
        {
            csharpTypeName = baseFieldType.Name.ToPascalCase();
        }


        var isList = allTypes.Any(e => e.Kind == TypeKind.List);
        var isNoneNull = allTypes.Any(e => e.Kind == TypeKind.NonNull);
        var csharpNullable = !isNoneNull && csharpType != null && csharpTypeName != "string";

        var graphTypeDefinition = isNoneNull ? baseFieldType.Name + "!" : baseFieldType.Name;
        if (isList)
        {
            graphTypeDefinition = $"[{graphTypeDefinition}]";
        }

        return new TypeInfo
        {
            Kind = baseFieldType.Kind,
            IsList = isList,
            IsNoneNull = isNoneNull,
            CSharpType = csharpType,
            CSharpTypeName = csharpTypeName,
            GraphTypeDefinition = graphTypeDefinition,
            IsEnum = baseFieldType.Kind == TypeKind.Enum
        };
    }

}



public class Field : BaseField
{
    public List<Arg> Args { get; set; }
    public bool IsMethod => Args != null && Args.Any();

    public bool SupportCursorPaging()
    {
        if (!GraphqlType.ContainPageInfo()) { return false; }
        if (Args?.FirstOrDefault(e => e.Name == "after" && e.FieldInfo.CSharpTypeName == "string") == null) { return false; }
        if (Args?.FirstOrDefault(e => e.Name == "before" && e.FieldInfo.CSharpTypeName == "string") == null) { return false; }
        return true;
    }

    public string GetArgNames()
    {
        if (Args == null) return null;
        return string.Join(", ", Args.Select(e => e.Name));
    }

    public string GetArgString(bool addTypeAttribute)
    {
        var result = "";

        if (!IsMethod)
        {
            return result;
        }

        foreach (var arg in Args.OrderByDescending(x => x.FieldInfo.IsNoneNull))
        {
            var baseType = arg.FieldInfo;
            if (result != "")
            {
                result += ", ";
            }

            if (addTypeAttribute)
            {
                result +=
                    $"[GraphArgument(\"{baseType.GraphTypeDefinition}\")] {baseType.CSharpTypeNameFull} {arg.Name.ToCamelCase()}";
            }
            else
            {
                result += $"{baseType.CSharpTypeNameFull} {arg.Name.ToCamelCase()}";
            }

            if (!baseType.IsNoneNull || baseType.IsList)
            {
                result += " = null";
            }
        }

        return result;
    }
}

public class TypeInfo
{
    public TypeKind Kind { get; set; }

    public string GraphTypeDefinition { get; set; }

    public string CSharpTypeName { get; set; }
    public Type CSharpType { get; set; }
    public bool IsNoneNull { get; set; }
    public bool IsList { get; set; }

    private bool csharpNullQuestion =>
        !IsNoneNull && (Kind == TypeKind.Enum || (CSharpType != null && CSharpTypeName != "string"));


    public string CSharpTypeNameFull
    {
        get
        {
            var result = CSharpTypeName + (csharpNullQuestion ? "?" : "");

            if (IsList)
            {
                return $"List<{result}>";
            }

            return result;
        }
    }

    public bool IsEnum { get; set; }
}


public class Arg : BaseField
{
    public string DefaultValue { get; set; }
}



public class BaseType
{
    public string Name { get; set; }
    public TypeKind Kind { get; set; }
    public string Description { get; set; }

    public string CSharpName => Name?.ToPascalCase();
    public string FileName => CSharpName + ".cs";

    public BaseType OfType { get; set; }



    public List<BaseType> GetAllTypes()
    {
        var result = new List<BaseType> { this };

        if (OfType != null)
        {
            result.AddRange(OfType.GetAllTypes());
        }

        return result;
    }

    public BaseType GetBaseBaseType()
    {
        if (OfType == null) return this;
        return OfType.GetBaseBaseType();
    }

}