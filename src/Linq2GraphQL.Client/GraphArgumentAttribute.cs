namespace Linq2GraphQL.Client;

[AttributeUsage(AttributeTargets.Parameter)]
public class GraphArgumentAttribute : Attribute
{
    public GraphArgumentAttribute(string graphType)
    {
        GraphType = graphType;
    }

    public string GraphType { get; private set; }
}