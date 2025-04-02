using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Linq2GraphQL.Client.Visitors
{
    internal class ExpressionNode
    {

        private List<ExpressionNode> childNodes = [];

        public List<ParameterExpression> ParameterExpressions { get; set; }

        public List<ExpressionMember> Members { get; set; } = [];

        public ExpressionNode ParentNode { get; set; }

        public List<ExpressionNode> ChildNodes => childNodes;

        public QueryNode PopulateQueryNode(QueryNode queryNode)
        {

            QueryNode currentNode = queryNode;

            //Add Members
            if (Members.Any())
            {
                var reversedMembers = Enumerable.Reverse(Members);
                var expressionMember = reversedMembers.First();
                var topNode = queryNode.AddChildNode(expressionMember.MemberInfo, expressionMember.GraphQLName, expressionMember.Arguments);

                currentNode = topNode;
                foreach (var member in reversedMembers.Skip(1))
                {
                    currentNode = currentNode.AddChildNode(member.MemberInfo, member.GraphQLName, member.Arguments);
                }

                //TODO Is this a good idea
                if (!ChildNodes.Any())
                {
                    currentNode.IncludePrimitive = true;
                }


            }

            foreach (var childNode in ChildNodes)
            {
                childNode.PopulateQueryNode(currentNode);
            }


            return queryNode;
        }


        private ExpressionNode GetTargetNode(ExpressionMember expressionMember)
        {
            if (ParentNode == null ||  expressionMember.ParameterExpression == null) { return this; }
        
            if (ParameterExpressions == null || !ParameterExpressions.Contains(expressionMember.ParameterExpression))
            {
                return ParentNode.GetTargetNode(expressionMember);
            }

            return this;

        }

        public ExpressionNode AddChild(ExpressionNode expressionNode)
        {
            expressionNode.ParentNode = this;
            childNodes.Add(expressionNode);
            return expressionNode;
        }

        public void AddMember(ExpressionMember expressionMember)
        {
            var target = GetTargetNode(expressionMember);
            target.Members.Add(expressionMember);

        }

    }

    internal class ExpressionMember
    {

        public ExpressionMember(string graphQLName, MemberInfo memberInfo, ParameterExpression parameterExpression, List<ArgumentValue> arguments = null)
        {
            GraphQLName = graphQLName;
            MemberInfo = memberInfo;
            Arguments = arguments;
            ParameterExpression = parameterExpression;
        }

        public string GraphQLName { get; set; }
        public MemberInfo MemberInfo { get; set; }
        public ParameterExpression ParameterExpression { get; set; }
        public List<ArgumentValue> Arguments { get; set; } = [];
    }
}
