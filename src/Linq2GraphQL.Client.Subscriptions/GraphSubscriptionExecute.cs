using System.Diagnostics;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace Linq2GraphQL.Client.Subscriptions;

public class GraphSubscriptionExecute<T, TResult> : GraphBaseExecute<T, TResult>
{
    public GraphSubscriptionExecute(GraphClient client, OperationType operationType, QueryNode queryNode,
        Expression<Func<T, TResult>> selector) : base(client, operationType, queryNode, selector) { }

    public async Task<IObservable<TResult>> StartAsync()
    {
        var request = await GetRequestAsync();

        if (string.IsNullOrWhiteSpace(client.SubscriptionUrl))
        {
            throw new Exception("Subscription url is not set");
        }

        if (client.SubscriptionProtocol == SubscriptionProtocol.ServerSentEvents)
        {
            var sseClient = new SSEClient(client, request);
#pragma warning disable CS4014
            Task.Run(sseClient.Start);
#pragma warning restore CS4014
            return sseClient.Subscription.SelectMany(json => SafeProcessMessage(json, request));
        }

        var wsClient = new WSClient(client, request);
        await wsClient.Start();
        return wsClient.Subscription.SelectMany(json => SafeProcessMessage(json, request));
    }

    private IEnumerable<TResult> SafeProcessMessage(string json, GraphQLRequest request)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<TResult>();
        }

        try
        {
            var result = queryExecutor.ProcessResponse(json, QueryNode.Name, request);
            return new[] { ConvertResult(result) };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Subscription message error: {ex.Message}");
            return Array.Empty<TResult>();
        }
    }
}