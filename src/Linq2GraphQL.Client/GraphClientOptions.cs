
namespace Linq2GraphQL.Client;

public class GraphClientOptions
{
    public bool UseSafeMode { get; set; } = false;
    public SubscriptionProtocol SubscriptionProtocol { get; set; } = default;
    public Func<GraphClient, Task<GraphQLRequest>> WSConnectionInitPayload { get; set; } = opts => null;
}