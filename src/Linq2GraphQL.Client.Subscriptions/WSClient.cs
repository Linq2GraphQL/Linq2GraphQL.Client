﻿using System.Diagnostics;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Serialization;
using Websocket.Client;

namespace Linq2GraphQL.Client.Subscriptions;

public class WSClient : IAsyncDisposable
{
    private readonly GraphClient _graphClient;
    private readonly GraphQLRequest payload;

    private readonly Subject<string> subscriptionSubject = new();
    private readonly WebsocketClient client;

    private readonly JsonSerializerOptions jsonOptions;

    public WSClient(GraphClient graphClient, GraphQLRequest payload)
    {
        _graphClient = graphClient;
        this.payload = payload;
        jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var factory = new Func<ClientWebSocket>(() =>
        {
            var ws = new ClientWebSocket();
            ws.Options.AddSubProtocol(GetSubprotocolString());
            return ws;
        });

        client = new WebsocketClient(new Uri(_graphClient.SubscriptionUrl), factory)
        {
            ReconnectTimeout = TimeSpan.FromSeconds(30)
        };
    }

    public IObservable<string> Subscription => subscriptionSubject.AsObservable();


    public async ValueTask DisposeAsync()
    {
        if (client == null)
        {
            return;
        }

        await client.Stop(WebSocketCloseStatus.NormalClosure, string.Empty);
        client.Dispose();
    }

    public async Task Start()
    {
        client.ReconnectionHappened.Subscribe(info => LogMessage($"Reconnection, type: {info.Type}"));

        //General log message
        client.MessageReceived.Subscribe(msg => LogMessage($"Message received: {msg}"));


        //Filter General response
        var tt = client.MessageReceived.Select(m => JsonSerializer.Deserialize<WebsocketResponse>(m.ToString()));

        tt.Where(e => e.Type == WebsocketRequestTypes.PING).Subscribe(msg => SendRequest(new WebsocketRequest(WebsocketRequestTypes.PONG)));

        tt.Where(e => !string.IsNullOrEmpty(e?.Id)).Subscribe(r =>
        {
            subscriptionSubject.OnNext(r.Payload?.ToString());
        });

        await client.Start();

        var initRequest = new WebsocketRequest(WebsocketRequestTypes.CONNECTION_INIT);
        if (_graphClient.WSConnectionInitPayload is not null) 
        {
            var initPayload = await _graphClient.WSConnectionInitPayload(_graphClient);
            if (initPayload is not null)
            {
                initRequest.Payload = initPayload;
            }
        }
        SendRequest(initRequest);

        var subscriptionRequest = new WebsocketRequest(GetSubscribeCommand())
        {
            Id = Guid.NewGuid().ToString(),
            Payload = payload
        };

        SendRequest(subscriptionRequest);
    }

    private string GetSubprotocolString()
    {
        switch (_graphClient.SubscriptionProtocol)
        {
            case SubscriptionProtocol.GraphQLWebSocket:
                return SubscriptionProtocols.GRAPGQL_TRANSPORT_WS;

            case SubscriptionProtocol.ApolloWebSocket:
                return SubscriptionProtocols.GRAPHQL_WS;

            default:
                throw new Exception($"{_graphClient.SubscriptionProtocol} is unknown");
        }
    }

    private string GetSubscribeCommand()
    {
        switch (_graphClient.SubscriptionProtocol)
        {
            case SubscriptionProtocol.GraphQLWebSocket:
                return SubscribeCommands.SUBSCRIBE;

            case SubscriptionProtocol.ApolloWebSocket:
                return SubscribeCommands.START;

            default:
                throw new Exception($"{_graphClient.SubscriptionProtocol} is unknown");
        }
    }

    private static void LogMessage(string message)
    {
        // Write logs to debug console
        Debug.WriteLine($"{message} - {DateTime.Now.ToString("T")}");
    }

    private void SendRequest(WebsocketRequest request)
    {
        var json = JsonSerializer.Serialize(request, jsonOptions);
        client.Send(json);
        LogMessage($"Message sent: {json}");
    }
}