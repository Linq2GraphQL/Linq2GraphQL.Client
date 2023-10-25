using System.Linq.Expressions;
using System.Reflection;

namespace Linq2GraphQL.Client;

public static class Utilities
{
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
                    ParseExpressionInternal(argument, parent);
                }
                break;
        }
    }

    private static void ParseMethodCallExpression(QueryNode parent, MethodCallExpression methodCallExp)
    {
        var graphMethodAttribute = methodCallExp.Method.GetCustomAttribute<GraphMethodAttribute>();

        if (graphMethodAttribute != null)
        {
            var arguments = new List<ArgumentValue>();

            var i = 0;
            foreach (var parameter in methodCallExp.Method.GetParameters())
            {
                var graphAtttribute = parameter.GetCustomAttribute<GraphArgumentAttribute>();
                if (graphAtttribute != null)
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

                    arguments.Add(new ArgumentValue(parameter.Name, graphAtttribute.GraphType,
                        argConstant.Value));
                }

                i++;
            }

            var queryNode = new QueryNode(methodCallExp.Method, graphMethodAttribute.GraphName, arguments);
            parent.AddChildNode(queryNode);

        }
        else if (methodCallExp.Method.Name == "Select" && methodCallExp.Arguments.Count == 2)
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

    public static (QueryNode ParentNode, QueryNode LastNode) GetMemberQueryNode(Expression expression)
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


    public static List<MemberInfo> GetMembers(Expression expression)
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