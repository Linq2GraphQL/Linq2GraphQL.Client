﻿using System.Runtime.Serialization;
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

    public bool IsDeprecated { get; set; }
    public string DeprecationReason { get; set; }

    public string SafeDeprecationReason => Helpers.SafeDeprecationReason(DeprecationReason);


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

    public string SafeName => Helpers.SafeVariableName(Name);
   

    public string Description { get; set; }

    public bool IsDeprecated { get; set; }
    public string DeprecationReason { get; set; }

    public string SafeDeprecationReason => Helpers.SafeDeprecationReason(DeprecationReason);

    public bool HasDescription => !string.IsNullOrEmpty(Description);

    public string SummaryDescription => Helpers.SummarySafe(Description);

    public string CSharpName => Name?.ToPascalCase();

    public GraphqlType GraphqlType { get; set; }


    public BaseType Type { get; set; }

    private CoreType coreType;
    public CoreType CoreType
    {
        get
        {
            coreType ??= Type.GetCoreType();
            return coreType;
        }
    }



}



public class Field : BaseField
{
    public List<Arg> Args { get; set; }
    public bool IsMethod => Args != null && Args.Any();

    public bool SupportCursorPaging()
    {
        if (!GraphqlType.ContainPageInfo()) { return false; }
        if (Args?.FirstOrDefault(e => e.Name == "after" && e.CoreType.CSharpTypeName == "string") == null) { return false; }
        if (Args?.FirstOrDefault(e => e.Name == "before" && e.CoreType.CSharpTypeName == "string") == null) { return false; }
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

        foreach (var arg in Args.OrderByDescending(x => x.CoreType.OuterNoneNull))
        {
            var coreType = arg.CoreType;
            if (result != "")
            {
                result += ", ";
            }

            if (addTypeAttribute)
            {
                result +=
                    $"[GraphQLArgument(\"{arg.Name}\", \"{arg.CoreType.GraphQLTypeDefinition}\")] {arg.CoreType.CSharpTypeDefinition} {arg.Name.ToCamelCase()}";
            }
            else
            {
                result += $"{arg.CoreType.CSharpTypeDefinition} {arg.Name.ToCamelCase()}";
            }

            if (!arg.CoreType.OuterNoneNull)
            {
                result += " = null";
            }
        }

        return result;
    }
}


public class Arg : BaseField
{
    public string DefaultValue { get; set; }
}



public class BaseType
{
    public string Name { get; set; }
    public string CSharpVariableName => Helpers.SafeVariableName(Name);
    public TypeKind Kind { get; set; }
    public string Description { get; set; }

    public bool HasDescription => !string.IsNullOrEmpty(Description);

    public string SummaryDescription => Helpers.SummarySafe(Description);

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


    public CoreType GetCoreType()
    {
        var result = new CoreType();

        bool currentNoneNull = false;

        foreach (var type in GetAllTypes())
        {
            switch (type.Kind)
            {
                case TypeKind.NonNull:
                    currentNoneNull = true;
                    break;
                case TypeKind.List:
                    result.Lists.Add(new CoreTypeList { NoneNull = currentNoneNull });
                    currentNoneNull = false;
                    break;
                default:

                    result.NoneNull = currentNoneNull;
                    result.BaseType = type;
                    currentNoneNull = false;
                    break;

            }
        }

        result.Lists.Reverse();

        if (Helpers.TypeMapping.TryGetValue(result.BaseType.Name, out var typeMapping))
        {
            result.CSharpTypeName = typeMapping.Name;
            result.CSharpType = typeMapping.type;
        }
        else
        {
            result.CSharpTypeName = result.BaseType.Name.ToPascalCase();
        }

        result.OuterNoneNull = result.Lists.FirstOrDefault()?.NoneNull ?? result.NoneNull;

        result.SetCSharpTypeDefinition();
        result.SetGraphQLTypeDefinition();

        return result;

    }


}


public class CoreTypeList
{
    public bool NoneNull { get; set; }
}

public class CoreType
{
    public BaseType BaseType { get; set; }
    public bool NoneNull { get; set; }
    public bool OuterNoneNull { get; set; }
    public string CSharpTypeName { get; set; }
    public Type CSharpType { get; set; }
    public List<CoreTypeList> Lists { get; set; } = [];
    public bool IsInList => Lists.Count != 0;

    public bool IsEnum => BaseType.Kind == TypeKind.Enum;

    public string CSharpTypeDefinition { get; set; }
    public string CSharpTypeDefinitionNeverNull
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(CSharpTypeDefinition) && CSharpTypeDefinition.EndsWith('?'))
            {
                return CSharpTypeDefinition.RemoveFromEnd("?");
            }
            return CSharpTypeDefinition;
        }
    }
    public string GraphQLTypeDefinition { get; set; }

    public void SetGraphQLTypeDefinition()
    {
        var result = BaseType.Name;

        if (NoneNull) { result += "!"; }

        foreach (var list in Lists)
        {
            result = $"[{result}]";
            if (list.NoneNull) { result += "!"; }
        }

        GraphQLTypeDefinition = result;

    }

    private bool UseSharpNoneNull()
    {

        if (GeneratorSettings.Current.Nullable || NoneNull)
        {
            return NoneNull;
        }

        //If Nullable
        if (BaseType.Kind == TypeKind.Enum || (CSharpType != null && CSharpTypeName != "string"))
        {
            return NoneNull;

            //            return NoneNull && !(BaseType.Kind == TypeKind.Enum || (CSharpType != null && CSharpTypeName != "string"));
        }

        return true;
    }


    public void SetCSharpTypeDefinition()
    {
        var result = CSharpTypeName;

        if (!UseSharpNoneNull()) { result += "?"; }

        foreach (var list in Lists)
        {
            result = $"List<{result}>";
            if (!list.NoneNull && GeneratorSettings.Current.Nullable) { result += "?"; }
        }

        CSharpTypeDefinition = result;

    }

}