namespace Linq2GraphQL.Client;

/// <summary>
/// Implemented by clients generated with <c>--agent</c>: the schema the client was generated from, as SDL.
/// Lets other packages require such a client at compile time.
/// </summary>
public interface IGraphSchemaSource
{
    static abstract string Sdl { get; }
}
