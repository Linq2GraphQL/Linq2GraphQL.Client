using Linq2GraphQL.Client;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Linq2GraphQL.Client.Visitors
{
    public class ParameterVisitor(MemberNode memberNode) : ExpressionVisitor
    {

        public MemberNode ParseExpression(Expression expression)
        {
            Visit(expression);
            return memberNode;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var attribute = node.Member.GetCustomAttribute<GraphQLMemberAttribute>();

            if (attribute != null)
            {
                var parameter = GetParameterExpression(node);
                AddMemberNodes(parameter, node);
            }

            return node;
        }

        public MemberNode AddMemberNodes(ParameterExpression targetParameter, Expression expression)
        {
            var targetNode = memberNode.GetMemberNodeFromParameterExpression(targetParameter);
            var newNode = targetNode.AddMembers(expression);
            return newNode;
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            var attribute = expression.Method.GetCustomAttribute<GraphQLMemberAttribute>();

            if (attribute != null)
            {
                var parExp = GetParameterExpression(expression);
                var i = 0;
                var argumentValues = new List<ArgumentValue>();
                foreach (var parameter in expression.Method.GetParameters())
                {
                    var graphQLArgumentAttribute = parameter.GetCustomAttribute<GraphQLArgumentAttribute>();
                    if (graphQLArgumentAttribute != null)
                    {
                        var arg = expression.Arguments[i];
                        var v = GetArgumentValue(arg);

                        argumentValues.Add(new ArgumentValue(graphQLArgumentAttribute.GraphQLName, graphQLArgumentAttribute.GraphQLType, GetArgumentValue(arg)));

                    }
                    i++;
                }

                var targetNode = memberNode.GetMemberNodeFromParameterExpression(parExp);
                targetNode.AddChild(new MemberNode(expression.Method, argumentValues));


                return expression;
            }

            if (IsLinqOperator(expression.Method))
            {
                var memberExp = expression.Arguments[0] as MemberExpression;
                var attr = memberExp?.Member.GetCustomAttribute<GraphQLMemberAttribute>();

                if (attr != null)
                {
                    var parameter = GetParameterExpression(expression.Arguments[1]);

                    var child = memberNode.AddMembers(memberExp);
                    child.SetParameterExpression(parameter);


                    var visitor = new ParameterVisitor(child);
                    visitor.ParseExpression(expression.Arguments[1]);
                    return expression;
                }

            }
            return base.VisitMethodCall(expression);
        }

        private static object GetArgumentValue(Expression element)
        {
            if (element is ConstantExpression)
            {
                return (element as ConstantExpression).Value;
            }

            var l = Expression.Lambda(Expression.Convert(element, element.Type));
            return l.Compile().DynamicInvoke();
        }


        private ParameterExpression GetParameterExpression(Expression expression)
        {
            if (expression == null)
            {
                return null;
            }

            if (expression.NodeType == ExpressionType.Parameter)
            {
                return expression as ParameterExpression;
            }
            else if (expression.NodeType == ExpressionType.MemberAccess)
            {
                var member = (MemberExpression)expression;
                return GetParameterExpression(member.Expression);
            }
            else if (expression.NodeType == ExpressionType.Lambda)
            {
                var member = (LambdaExpression)expression;
                return GetParameterExpression(member.Parameters[0]);
            }
            else if (expression.NodeType == ExpressionType.Call)
            {
                var member = (MethodCallExpression)expression;

                if (member.Object != null)
                {
                    return GetParameterExpression(member.Object);
                }

                return GetParameterExpression(member.Arguments[0]);

            }


            return null;
        }

        private static bool IsLinqOperator(MethodInfo method)
        {
            if (method.DeclaringType != typeof(Queryable) && method.DeclaringType != typeof(Enumerable))
                return false;
            return Attribute.GetCustomAttribute(method, typeof(ExtensionAttribute)) != null;
        }

    }


}
