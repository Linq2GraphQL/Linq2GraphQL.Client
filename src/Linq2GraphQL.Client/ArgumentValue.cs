namespace Linq2GraphQL.Client;

public record ArgumentValue
{
    public ArgumentValue(string graphName, string graphType, object value)
    {
        GraphType = graphType;
        Value = value;
        GraphName = graphName;
        VariableName = graphName;
    }

    public string GraphName { get; init; }
    public string GraphType { get; init; }
    public object Value { get; init; }
    public string VariableName { get; set; } // mutable: unique suffix appended during query building
}