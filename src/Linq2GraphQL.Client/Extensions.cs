using System.Reflection;
using System;

namespace Linq2GraphQL.Client;

internal static class Extensions
{
    internal static bool IsValueTypeOrString(this Type type)
    {
        if (type == null)
        {
            return false;
        }

        return type.IsValueType || type == typeof(string);
    }

    internal static bool IsListOfPrimitiveTypeOrString(this Type type)
    {
        return GetTypeOrListType(type).IsValueTypeOrString();
    }

    private static bool IsList(this Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
    }

    internal static Type GetTypeOrListType(this Type type)
    {
        if (type.IsList())
        {
            var genericArguments = type.GetGenericArguments();

            return genericArguments[0].GetTypeOrListType();
        }

        return type;
    }

    internal static string ToCamelCase(this string input)
    {
        if (char.IsLower(input[0]))
        {
            return input;
        }

        return string.Concat(input[..1].ToLower(), input.AsSpan(1));
    }

    internal static Type GetUnderlyingType(this MemberInfo member)
    {
        return member.MemberType switch
        {
            MemberTypes.TypeInfo => (Type)member,
            MemberTypes.Event => ((EventInfo)member).EventHandlerType,
            MemberTypes.Field => ((FieldInfo)member).FieldType,
            MemberTypes.Method => ((MethodInfo)member).ReturnType,
            MemberTypes.Property => ((PropertyInfo)member).PropertyType,
            _ => throw new ArgumentException
                            (
                                "Input MemberInfo must be if type TypeInfo, EventInfo, FieldInfo, MethodInfo, or PropertyInfo"
                            ),
        };
    }
}