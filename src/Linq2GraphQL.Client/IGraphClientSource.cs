namespace Linq2GraphQL.Client;

/// <summary>
/// Implemented by every generated client: access to its <see cref="Client.GraphClient"/>, and a way to derive a
/// client of the same type with an extra request hook. Lets other packages work with any generated client
/// generically, for example to execute queries with a per-user token.
/// </summary>
public interface IGraphClientSource<TSelf> where TSelf : IGraphClientSource<TSelf>
{
    GraphClient GraphClient { get; }

    TSelf WithRequestConfiguration(Func<HttpRequestMessage, CancellationToken, ValueTask> configureRequest);
}
