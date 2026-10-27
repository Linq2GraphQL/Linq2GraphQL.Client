namespace Linq2GraphQL.Generator;

/// <summary>
///     A scalar of a schema and the C# type the generator maps it to when no scalarMappings override applies.
/// </summary>
public class ScalarInfo
{
    public string Name { get; init; }

    public string Description { get; init; }

    /// <summary>
    ///     GraphQL's own Int, Float, String and Boolean (see <see cref="Helpers.BuiltInScalars" />). They can be
    ///     mapped to another type but not opted out to a CustomScalar class.
    /// </summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>
    ///     The C# type name emitted for the scalar, or null when a CustomScalar class is generated for it.
    /// </summary>
    public string DefaultTarget { get; init; }
}
