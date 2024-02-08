using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;

namespace Linq2GraphQL.Client;

public static class Utilities
{
    public static string GetArgumentsId(IEnumerable<object> objects)
    {
        if (objects == null) return null;
        var objs = objects.Where(o => o != null);
        if (!objs.Any()) return null;

        unchecked
        {
            var hash = 19;
            foreach (var obj in objs)
            {
                hash = hash * 31 + obj.GetHashCode();
            }

            return hash.ToString().Replace("-", "_");
        }
    }

    private static bool IsSelectOrSelectMany(this MethodCallExpression methodCallExpression)
    {
        if (methodCallExpression.Arguments.Count != 2)
        {
            return false;
        }

        ;
        var methodName = methodCallExpression.Method.Name;
        return (methodName == "Select" || methodName == "SelectMany");
    }

    public static void ParseExpression(Expression body, QueryNode parent)
    {
        var node = new QueryNode(parent.Member);
        ParseExpressionInternal(body, node);
        node.SetAddPrimitiveChildren();

        foreach (var childNode in node.ChildNodes)
        {
            parent.AddChildNode(childNode);
        }
    }

    private static void ParseExpressionInternal(Expression body, QueryNode parent)
    {
        if (body.NodeType == ExpressionType.MemberInit)
        {
            var exp = (MemberInitExpression)body;
            foreach (var binding in exp.Bindings.Where(e => e.BindingType == MemberBindingType.Assignment)
                         .Cast<MemberAssignment>())
            {
                ParseExpressionInternal(binding.Expression, parent);
            }
        }

        switch (body)
        {
            case LambdaExpression lambdaExpression:
                ParseExpressionInternal(lambdaExpression.Body, parent);
                break;

            case MemberExpression memberExpression:
                var (parentNode, _) = GetMemberQueryNode(memberExpression);
                parent.AddChildNode(parentNode);
                break;

            case MethodCallExpression methodCallExp:
                ParseMethodCallExpression(parent, methodCallExp);
                break;

            case NewExpression newExpression:
                foreach (var argument in newExpression.Arguments)
                {
                    ParseExpression(argument, parent);
                }

                break;
        }
    }

    private static void ParseMethodCallExpression(QueryNode parent, MethodCallExpression methodCallExp)
    {
        var graphInterfaceAttribute = methodCallExp.Method.GetCustomAttribute<GraphInterfaceAttribute>();
        if (graphInterfaceAttribute != null)
        {
            var queryNode = new QueryNode(methodCallExp.Method, methodCallExp.Method.Name, null, true);
            parent.AddChildNode(queryNode);
            return;
        }

        var graphMethodAttribute = methodCallExp.Method.GetCustomAttribute<GraphMethodAttribute>();
        if (graphMethodAttribute != null)
        {
            var arguments = new List<ArgumentValue>();

            var i = 0;
            foreach (var parameter in methodCallExp.Method.GetParameters())
            {
                var graphAttribute = parameter.GetCustomAttribute<GraphArgumentAttribute>();
                if (graphAttribute != null)
                {
                    var arg = methodCallExp.Arguments[i];
                    ConstantExpression argConstant;
                    if (arg.NodeType == ExpressionType.Convert)
                    {
                        var unaryExpression = (UnaryExpression)arg;
                        argConstant = (ConstantExpression)unaryExpression.Operand;
                    }
                    else
                    {
                        argConstant = (ConstantExpression)arg;
                    }

                    arguments.Add(new ArgumentValue(parameter.Name, graphAttribute.GraphType,
                        argConstant.Value));
                }

                i++;
            }

            var queryNode = new QueryNode(methodCallExp.Method, graphMethodAttribute.GraphName, arguments);
            parent.AddChildNode(queryNode);
        }
        else if (methodCallExp.IsSelectOrSelectMany())
        {
            if (methodCallExp.Arguments[0] is MemberExpression memberExpr)
            {
                var (ParentNode, LastNode) = GetMemberQueryNode(memberExpr);
                ParseExpressionInternal(methodCallExp.Arguments[1], LastNode);
                parent.AddChildNode(ParentNode);
            }
            else
            {
                ParseExpressionInternal(methodCallExp.Arguments[1], parent);
            }
        }
    }

    private static (QueryNode ParentNode, QueryNode LastNode) GetMemberQueryNode(Expression expression)
    {
        var members = GetMembers(expression);
        if (members == null) return (null, null);

        members.Reverse();

        QueryNode parentNode = null;
        QueryNode currentNode = null;

        foreach (var member in members)
        {
            var newNode = new QueryNode(member);
            if (parentNode == null)
            {
                parentNode = newNode;
            }
            else
            {
                currentNode.AddChildNode(newNode);
            }

            currentNode = newNode;
        }

        return (parentNode, currentNode);
    }


    private static List<MemberInfo> GetMembers(Expression expression)
    {
        var members = new List<MemberInfo>();
        if (expression.NodeType == ExpressionType.MemberAccess)
        {
            var memberExpression = (MemberExpression)expression;
            members.Add(memberExpression.Member);
            members.AddRange(GetMembers(memberExpression.Expression));
        }

        return members;
    }
}