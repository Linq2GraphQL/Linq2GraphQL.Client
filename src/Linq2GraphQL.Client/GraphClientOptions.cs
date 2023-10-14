namespace Linq2GraphQL.Client;

public class GraphClientOptions
{
    public bool UseSafeMode { get; set; } = false;
    public SubscriptionProtocol SubscriptionProtocol { get; set; } = default;
}