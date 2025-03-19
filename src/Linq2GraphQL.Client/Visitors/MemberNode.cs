using Linq2GraphQL.Client;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Linq2GraphQL.Client.Visitors
{
    internal class MemberNode(MemberInfo memberInfo, List<ArgumentValue> arguments = null, ParameterExpression parameterExpression = null)
    {
        public string ParameterName => parameterExpression?.Name;

        public MemberInfo MemberInfo => memberInfo;
        public MemberNode Parent { get; set; }
        public List<MemberNode> Children { get; set; } = [];
        public List<ArgumentValue> Arguments => arguments;

        public void SetParameterExpression(ParameterExpression param)
        {
            parameterExpression = param;
        }


        public bool HasArguments => arguments != null && arguments.Count > 0;

        public MemberNode GetMemberNodeFromParameterExpression(ParameterExpression expression)
        {
            if (parameterExpression == expression || Parent == null) return this;
            return Parent.GetMemberNodeFromParameterExpression(expression);
        }


        public void AddChild(MemberNode memberNode)
        {
            memberNode.Parent = this;
            Children.Add(memberNode);
        }

        public MemberNode AddMembers(Expression node)
        {
            var members = GetMembers(node);
            members.Reverse();

            if (members.Count == 0) { return null; }

            var topNode = new MemberNode(members[0]);
            var currentNode = topNode;
            foreach (var member in members.Skip(1))
            {
                var newNode = new MemberNode(member);
                currentNode.AddChild(newNode);
                currentNode = newNode;
            }
            AddChild(topNode);

            return currentNode;

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

        public void PopulateChildQueryNodes(QueryNode queryNode)
        {
            foreach (var child in Children)
            {
                var childNode = new QueryNode(child.MemberInfo, null, child.Arguments);
                childNode.IncludePrimitive = child.Children.Count == 0;
                var addedNode = queryNode.AddChildNode(childNode);
                child.PopulateChildQueryNodes(addedNode);
            }
        }


        public string PrintMemberTree(int level = 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Level: {level}");

            level++;

            if (HasArguments)
            {
                var argumentList = $"({string.Join(", ", Arguments.Select(e => e.Value))})";

                sb.AppendLine($"Member: {MemberInfo?.Name}{argumentList}");
            }
            else
            {
                sb.AppendLine($"Member: {MemberInfo?.Name} ");
            }

            sb.AppendLine($"Parent: {Parent?.MemberInfo?.Name}");
            sb.AppendLine($"Parameter: {parameterExpression?.Name}");
           
            sb.AppendLine("");

            foreach (var child in Children)
            {
                sb.Append(child.PrintMemberTree(level));
            }

            sb.AppendLine("".PadLeft(level, '-'));

            return sb.ToString();

        }


    }


}
