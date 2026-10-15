namespace Linq2GraphQL.Client.AI;

/// <summary>
/// The agent configuration does not match the schema, for example an allowed operation or an omitted field
/// that does not exist. Thrown when the agent tools are set up, so mistakes surface at startup rather than
/// on the agent's first call.
/// </summary>
public class AgentConfigurationException : Exception
{
    public AgentConfigurationException(string message) : base(message)
    {
    }

    public AgentConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
