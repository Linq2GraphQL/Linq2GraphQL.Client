namespace Linq2GraphQL.Tests;

/// <summary>
/// The generator keeps per-run settings in the static <c>GeneratorSettings.Current</c>, so test classes that
/// run it must not run in parallel with each other.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class GeneratorCollection
{
    public const string Name = "Generator";
}
