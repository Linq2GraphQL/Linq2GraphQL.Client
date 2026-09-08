namespace Linq2GraphQL.Generator;

/// <summary>
/// Thrown when the generator configuration - the settings file or the command line that layers
/// over it - is invalid. These are user mistakes, so <c>Program</c> reports the message on its
/// own rather than dumping a stack trace.
/// </summary>
public class GeneratorConfigurationException : Exception
{
    public GeneratorConfigurationException(string message) : base(message)
    {
    }

    public GeneratorConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
