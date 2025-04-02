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

        public QueryNode PopulateQueryNode(QueryNode queryNode)
        {

            QueryNode currentNode = queryNode;

            //Add Members
            if (Members.Any())
            {
                var reversedMembers = Enumerable.Reverse(Members);

                var myMember = reversedMembers.First();
                var topNode = queryNode.AddChildNode(myMember.MemberInfo, myMember.GraphQLName, myMember.Arguments);
                currentNode = topNode;
                foreach (var member in reversedMembers.Skip(1))
                {
                    currentNode = currentNode.AddChildNode(member.MemberInfo, member.GraphQLName, member.Arguments);
                }

                //TODO Is this a good idea
                if(!ChildNodes.Any())
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


        public List<ParameterExpression> ParameterExpressions { get; set; }

        public List<ExpressionMember> Members { get; set; } = [];

        public List<ExpressionNode> ChildNodes { get; set; } = [];

    }

    internal class ExpressionMember
    {

        public ExpressionMember(string graphQLName, MemberInfo memberInfo, List<ArgumentValue> arguments = null)
        {
            GraphQLName = graphQLName;
            MemberInfo = memberInfo;
            Arguments = arguments;
        }

        public string GraphQLName { get; set; }
        public MemberInfo MemberInfo { get; set; }
        public List<ArgumentValue> Arguments { get; set; } = [];
    }
}
