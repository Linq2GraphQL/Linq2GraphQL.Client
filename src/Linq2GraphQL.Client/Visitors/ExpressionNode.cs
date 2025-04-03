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
    [DebuggerDisplay("Members = {Members}, Children = {ChildNodes}")]
    internal class ExpressionNode
    {

        private ExpressionParser expressionParser;





        public ExpressionNode(ExpressionNode parent, Expression expression, List<ParameterExpression> parameterExpressions = null)
        {
            ParentNode = parent;
            ParameterExpressions = parameterExpressions;
            expressionParser = new ExpressionParser(this);
            expressionParser.Parse(expression);
        }



        private List<ExpressionNode> childNodes = [];

        public int Level { get; set; }

        public List<ParameterExpression> ParameterExpressions { get; set; }

        public List<ExpressionMember> Members { get; set; } = [];

        public ExpressionNode ParentNode { get; set; }

        public List<ExpressionNode> ChildNodes => childNodes;

        public QueryNode PopulateQueryNode(QueryNode queryNode)
        {

            QueryNode currentNode = queryNode;


            foreach (var member in Members)
            {
                currentNode = queryNode.AddChildNode(member.MemberInfo, member.GraphQLName, member.Arguments);
            }


            //Add Members
            //if (Members.Any())
            //{
            //    var reversedMembers = Enumerable.Reverse(Members);
            //    var expressionMember = reversedMembers.First();
            //    var topNode = queryNode.AddChildNode(expressionMember.MemberInfo, expressionMember.GraphQLName, expressionMember.Arguments);

            //    currentNode = topNode;
            //    foreach (var member in reversedMembers.Skip(1))
            //    {
            //        currentNode = currentNode.AddChildNode(member.MemberInfo, member.GraphQLName, member.Arguments);
            //    }

            //TODO Is this a good idea
            if (!ChildNodes.Any())
            {
                currentNode.IncludePrimitive = true;
            }


            //}

            foreach (var childNode in ChildNodes)
            {
                childNode.PopulateQueryNode(currentNode);
            }


            return queryNode;
        }


        private ExpressionNode GetTargetNode(ExpressionMember expressionMember)
        {
            if (ParentNode == null || expressionMember.ParameterExpression == null) { return this; }

            if (ParameterExpressions == null || !ParameterExpressions.Contains(expressionMember.ParameterExpression))
            {
                return ParentNode.GetTargetNode(expressionMember);
            }

            return this;

        }

        public ExpressionNode AddChild(Expression expression, List<ParameterExpression> parameterExpressions = null)
        {
            var newNode = new ExpressionNode(this, expression, parameterExpressions);
            newNode.Level = Level + 1;

            childNodes.Add(newNode);
            return newNode;
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

        public void AddParent(ExpressionMember parent)
        {
            Parent = parent;
            parent.Child = this;
        }

        public ExpressionMember GetTopParent()
        {
            if (Parent == null) { return this; }
            return Parent.GetTopParent();
        }

        public ExpressionMember Parent { get; set; }
        public ExpressionMember Child { get; set; }


        public string GraphQLName { get; set; }
        public MemberInfo MemberInfo { get; set; }
        public ParameterExpression ParameterExpression { get; set; }
        public List<ArgumentValue> Arguments { get; set; } = [];
    }
}
