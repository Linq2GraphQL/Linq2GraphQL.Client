namespace Linq2GraphQL.Client;

public class ArgumentValue
{
    public ArgumentValue(string graphName, string graphType, object value)
    {
        GraphType = graphType;
        Value = value;
        GraphName = graphName;
        VariableName = graphName;
    }

    public string GraphName { get; set; }
    public string GraphType { get; set; }
    public object Value { get; set; }
    public string VariableName { get; set; }
}