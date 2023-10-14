namespace Linq2GraphQL.Client;

[AttributeUsage(AttributeTargets.Method)]
public class GraphMethodAttribute : Attribute
{
    public GraphMethodAttribute(string graphName)
    {
        GraphName = graphName;
    }

    public string GraphName { get; private set; }
}