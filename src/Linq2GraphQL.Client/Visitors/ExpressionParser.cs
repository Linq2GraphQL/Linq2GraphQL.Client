using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Linq2GraphQL.Client.Visitors
{
    internal class ExpressionParser(ExpressionNode expressionNode)
    {



        public ExpressionNode Parse(Expression expression)
        {
            //expressionNode.ParameterExpressions = parameterExpressions;
            ParseExpression(expression);
            return expressionNode;
        }


        private void ParseExpression(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Lambda:
                    EvaluateLambda(expression as LambdaExpression);
                    break;


                case ExpressionType.MemberAccess:
                    EvaluateMember(expression as MemberExpression);
                    break;

                case ExpressionType.Call:
                    EvaluateMethodCall(expression as MethodCallExpression);
                    break;

                case ExpressionType.MemberInit:
                    EvaluateInit(expression as MemberInitExpression);
                    break;

                case ExpressionType.New:
                    EvaluateNew(expression as NewExpression);
                    break;

            }
        }


        private void EvaluateLambda(LambdaExpression lambdaExpression)
        {
            Debug.WriteLine("Lambda: " + lambdaExpression.ToString());
            var parameterExpressions = lambdaExpression.Parameters.ToList();
            expressionNode.ParameterExpressions ??= parameterExpressions;

            Debug.WriteLine("ParameterExp: " + string.Join(",", expressionNode?.ParameterExpressions?.Select(e => e.Name) ?? []));

            if (expressionNode.ParameterExpressions == null)
            {
                ParseExpression(lambdaExpression.Body);
            }
            else
            {
                Debug.WriteLine("----------------------------");
                expressionNode.AddChild(lambdaExpression.Body, parameterExpressions);
            }

        }


        private void EvaluateInit(MemberInitExpression memberInitExpression)
        {
            Debug.WriteLine("Init: " + memberInitExpression);

            foreach (var binding in memberInitExpression.Bindings)
            {
                if (binding is MemberAssignment memberAssignment)
                {
                    expressionNode.AddChild(memberAssignment.Expression, expressionNode.ParameterExpressions);
                }
            }
        }

        private void EvaluateNew(NewExpression newExpression)
        {
            Debug.WriteLine("New: " + newExpression);

            // expressionNode.ParameterExpressions ??= newExpression.Arguments;

            foreach (var argumentExpression in newExpression.Arguments)
            {
                expressionNode.AddChild(argumentExpression, expressionNode.ParameterExpressions);
            }
        }

        private void EvaluateMember(MemberExpression memberExpression)
        {
            var member = GetExpressionMember(memberExpression)?.GetTopParent();
            if (member != null)
            {
                expressionNode.AddMember(member);
            }
        }


        private ExpressionMember GetExpressionMember(MemberExpression memberExpression)
        {
            var attr = memberExpression.Member.GetCustomAttribute<GraphQLMemberAttribute>();
            if (attr != null)
            {
                var member = new ExpressionMember(attr.GraphQLName, memberExpression.Member, null);

                if (memberExpression.Expression is ParameterExpression parameterExpression)
                {
                    member.ParameterExpression = parameterExpression;
                    return member;
                }

                if (memberExpression.Expression is not MemberExpression parentExpression) { return member; }
                var parent = GetExpressionMember(parentExpression);
                if (parent == null) { return member; }

                member.AddParent(parent);


                return member;
            }


            return null;
        }


        private void EvaluateMethodCall(MethodCallExpression methodCallExpression)
        {
            var attr = methodCallExpression.Method.GetCustomAttribute<GraphQLMemberAttribute>();

            if (attr != null)
            {
                Debug.WriteLine("Method: " + attr.GraphQLName);
                Debug.WriteLine("ParameterExp: " + string.Join(",", expressionNode?.ParameterExpressions?.Select(e => e.Name) ?? []));
                var i = 0;
                var argumentValues = new List<ArgumentValue>();
                foreach (var parameter in methodCallExpression.Method.GetParameters())
                {
                    var graphQLArgumentAttribute = parameter.GetCustomAttribute<GraphQLArgumentAttribute>();
                    if (graphQLArgumentAttribute != null)
                    {
                        var arg = methodCallExpression.Arguments[i];
                        var v = GetArgumentValue(arg);
                        argumentValues.Add(new ArgumentValue(graphQLArgumentAttribute.GraphQLName, graphQLArgumentAttribute.GraphQLType, GetArgumentValue(arg)));
                    }
                    i++;
                }

                var ll = methodCallExpression.Arguments.Where(e => e.NodeType == ExpressionType.Parameter).Cast<ParameterExpression>().FirstOrDefault();



                expressionNode.AddMember(new ExpressionMember(attr.GraphQLName, methodCallExpression.Method, ll, argumentValues)); //TODO Get ParameterExpression

            }
            else
            {
                foreach (var arg in methodCallExpression.Arguments)
                {
                    ParseExpression(arg);
                }
            }





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


    }
}
