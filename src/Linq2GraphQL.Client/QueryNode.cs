﻿using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Schema;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Linq2GraphQL.Client;

[DebuggerDisplay("Name = {Name}, Args = {Arguments}")]
public class QueryNode
{
    private readonly bool mustHaveChildren;
    private readonly Type underlyingMemberType;
    private string argumentHashCodeId;

    public QueryNode(MemberInfo member, string name = null, List<ArgumentValue> arguments = null, bool interfaceProperty = false, bool topLevel = false)
    {
        Name = name ?? member.GetCustomAttribute<GraphQLMemberAttribute>()?.GraphQLName ?? member.Name.ToCamelCase();
        Member = member;
        Arguments = arguments ?? new List<ArgumentValue>();
        underlyingMemberType = member.GetUnderlyingType();
        mustHaveChildren = MustHaveChildren(underlyingMemberType);
        InterfaceProperty = interfaceProperty;

        if (!topLevel) {
            SetArgumentHashCodeId();
        }
    }

    public bool InterfaceProperty { get; internal set; }
    public string Name { get; internal set; }
    public string Alias { get; internal set; }
   
    public MemberInfo Member { get; internal set; }
    public List<QueryNode> ChildNodes { get; internal set; } = new();
    public List<ArgumentValue> Arguments { get; internal set; } = new();
    public bool IncludePrimitive { get; internal set; }

    public QueryNode Parent { get; private set; }


    internal void IncludePrimitiveIfNoChildPrimitive()
    {
        if (!ChildNodes.Any(e => e.underlyingMemberType.IsValueTypeOrString()))
        {
            IncludePrimitive = true;
        }
    }

    private void SetArgumentHashCodeId()
    {
       argumentHashCodeId = Utilities.GetArgumentsId(Arguments?.Select(e=> e.Value));
        if (argumentHashCodeId != null)
        {
            Alias = Name + argumentHashCodeId;
        }
    }


    private static bool MustHaveChildren(Type type)
    {
        return !type.IsValueTypeOrString() &&
               !type.IsListOfPrimitiveTypeOrString();
    }


    public QueryNode AddChildNode(MemberInfo member, string name = null, List<ArgumentValue> arguments = null)
    {
        return AddChildNode(new QueryNode(member, name, arguments));
    }

    public int Level => Parent?.Level + 1 ?? 1;
    public int Leaf { get; internal set; } = 1;

    public QueryNode AddChildNode(QueryNode childNode)
    {
        var currentNode = ChildNodes.FirstOrDefault(e => e.Name == childNode.Name && e.argumentHashCodeId == childNode.argumentHashCodeId);
        if (currentNode == null)
        {
            childNode.Parent = this;
            childNode.Leaf = ChildNodes.Count + 1;
            ChildNodes.Add(childNode);
            return childNode;
        }
        else if (childNode.IncludePrimitive)
        {
            currentNode.IncludePrimitive = true;
        }

        foreach (var child in childNode.ChildNodes)
        {
            currentNode.AddChildNode(child);
        }

        return currentNode;

    }

    public void SetArgumentValue(string graphName, object value)
    {
        var argument = Arguments.FirstOrDefault(e => e.GraphName == graphName);
        if (argument != null)
        {
            argument.Value = value;
        }
    }

    public void AddPrimitiveChildren(bool recursive, GraphQLSchema schema)
    {
        if (recursive)
        {
            foreach (var child in ChildNodes.Where(e => e.mustHaveChildren))
            {
                child.AddPrimitiveChildren(recursive, schema);
            }
        }

        if (mustHaveChildren && (!ChildNodes.Any() || IncludePrimitive))
        {
            var typeOrListType = underlyingMemberType.GetTypeOrListType();
            foreach (var propertyInfo in typeOrListType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {

                if (!propertyInfo.PropertyType.IsValueTypeOrString())
                {
                    continue;
                }


                var memberAttribute = propertyInfo.GetCustomAttribute<GraphQLMemberAttribute>();
                if (memberAttribute == null)
                {
                    continue;
                }


                if (schema != null)
                {
                    if (schema.TypePropertyExists(typeOrListType.Name, memberAttribute.GraphQLName))
                    {
                        AddChildNode(propertyInfo, memberAttribute.GraphQLName);
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Property: {propertyInfo.Name} Type: {Member.Name} was not present in active schema");
                    }
                }
                else
                {
                    AddChildNode(propertyInfo);
                }
            }
        }
    }

    internal void SetAllUniqueVariableNames()
    {
        foreach (var argument in Arguments)
        {
            argument.VariableName += $"_{Level}_{Leaf}";
        }

        foreach (var child in ChildNodes)
        {
            child.SetAllUniqueVariableNames();
        }

    }

    public List<ArgumentValue> GetActiveArguments()
    {
        return Arguments.Where(e => e.Value != null).ToList();
    }

    public List<ArgumentValue> GetAllActiveArguments()
    {
        var allArguments = GetActiveArguments();
        foreach (var childNode in ChildNodes)
        {
            allArguments.AddRange(childNode.GetAllActiveArguments());
        }
        return allArguments;
    }

    private string GetArgumentString()
    {
        var args = GetActiveArguments(); ;
        if (!args.Any())
        {
            return "";
        }

        var argString = "(";
        foreach (var arg in args)
        {
            argString += arg.GraphName + ":$" + arg.VariableName + " ";
        }
        argString.TrimEnd();
        argString += ")";

        return argString;
    }




    internal string GetQueryString(string indent = "")
    {
        string query;
        var memberType = Member.GetUnderlyingType();

        if (InterfaceProperty)
        {
            query = "... on " + Name + GetArgumentString() + Environment.NewLine;

        }
        else if (!string.IsNullOrWhiteSpace(Alias))
        {
            query = Alias + ":" + Name + GetArgumentString() + Environment.NewLine;
        }
        else
        {
            query = Name + GetArgumentString() + Environment.NewLine;
        }

        if (memberType.IsListOfPrimitiveTypeOrString())
        {
            return indent + query;
        }

        query += indent + "{" + Environment.NewLine;

        if (ChildNodes.Any())
        {
            var newIndent = "  " + indent;
            foreach (var childNode in ChildNodes)
            {
                query += childNode.GetQueryString(newIndent);
            }
        }

        query += indent + "}" + Environment.NewLine;
        return query;
    }
}