using System.Reflection;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client.Schema;

namespace Linq2GraphQL.Client;

public class QueryNode
{
    private readonly bool mustHaveChildren;
    private readonly Type underlyingMemberType;

    public QueryNode(MemberInfo member, string alias = null, List<ArgumentValue> arguments = null)
    {
        Member = member;
        Alias = alias ?? member.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? Member.Name.ToCamelCase();
        Arguments = arguments ?? new List<ArgumentValue>();
        underlyingMemberType = member.GetUnderlyingType();
        mustHaveChildren = MustHaveChildren(underlyingMemberType);
    }

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

    private static bool MustHaveChildren(Type type)
    {
        return !type.IsValueTypeOrString() &&
               !type.IsListOfPrimitiveTypeOrString();
    }

    public void SetAddPrimitiveChildren()
    {
        if (!ChildNodes.Any())
        {
            IncludePrimitive = true;
        }
        foreach (var childNode in ChildNodes)
        {
            childNode.SetAddPrimitiveChildren();
        }
    }

    public void AddChildNode(MemberInfo member, string alias = null)
    {
        AddChildNode(new QueryNode(member, alias));
    }

    public int Level => Parent?.Level + 1 ?? 1;
    public int Leaf { get; internal set; } = 1;

    public void AddChildNode(QueryNode childNode)
    {
        var currentNode = ChildNodes.FirstOrDefault(e => e.Alias == childNode.Alias);
        if (currentNode == null)
        {
            childNode.Parent = this;
            childNode.Leaf = ChildNodes.Count + 1;
            ChildNodes.Add(childNode);
            return;
        }
        else if (childNode.IncludePrimitive)
        {
            currentNode.IncludePrimitive = true;
        }

        foreach (var child in childNode.ChildNodes)
        {
            currentNode.AddChildNode(child);
        }
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
                if (propertyInfo.GetCustomAttribute<GraphQLShadowPropertyAttribute>() != null)
                {
                    continue;
                }

                if (!propertyInfo.PropertyType.IsValueTypeOrString() || propertyInfo.GetCustomAttribute<GraphQLShadowPropertyAttribute>() != null)
                {
                    continue;
                }

                if (schema != null)
                {
                    var alias = propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ??
                                Member.Name.ToCamelCase();
                    if (schema.TypePropertyExists(typeOrListType.Name, alias))
                    {
                        AddChildNode(propertyInfo, alias);
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

        argString += ")";

        return argString;
    }




    internal string GetQueryString()
    {
        var indent = new string(' ', Level + 3);

        var memberType = Member.GetUnderlyingType();
        var query = Alias + GetArgumentString() + Environment.NewLine;

        if (memberType.IsListOfPrimitiveTypeOrString())
        {
            return query;
        }

        query += indent + "{" + Environment.NewLine;

        if (ChildNodes.Any())
        {
            foreach (var childNode in ChildNodes)
            {
                query += indent + childNode.GetQueryString();
            }
        }

        query += indent + "}" + Environment.NewLine;
        return query;
    }
}