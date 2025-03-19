using Linq2GraphQL.Client.Visitors;
using System.Linq.Expressions;
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

    public static void ParseExpression(Expression body, QueryNode parent)
    {
        var parameterVisitor = new ParameterVisitor(new MemberNode(null, null));
        var topNode = parameterVisitor.ParseExpression(body);
      
        topNode.PopulateChildQueryNodes(parent);
        
    }

}