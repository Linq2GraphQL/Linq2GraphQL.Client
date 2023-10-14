namespace Linq2GraphQL.Client;

public enum SubscriptionProtocol
{
    /// <summary>
    ///     Default protocol
    ///     actually named graphql-ws just to make things even more confusing
    ///     sends: graphql-transport-ws as subprotocol
    /// </summary>
    GraphQLWebSocket,

    /// <summary>
    ///     Old apollo protocol
    ///     sends: graphql-ws as subprotocol
    /// </summary>
    ApolloWebSocket,

    /// <summary>
    ///     SSE
    /// </summary>
    ServerSentEvents
}