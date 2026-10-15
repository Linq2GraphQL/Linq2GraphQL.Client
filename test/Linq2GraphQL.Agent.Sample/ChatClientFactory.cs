using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

namespace Linq2GraphQL.Agent.Sample;

/// <summary>
/// Builds the chat client from the "Agent" configuration section. Nothing secret lives in the repository:
/// Azure AI Foundry signs in with your own Entra ID account through DefaultAzureCredential (az login, Visual
/// Studio), and an API key, when one is needed, comes from user secrets or an environment variable.
/// </summary>
internal static class ChatClientFactory
{
    private const string CognitiveServicesScope = "https://cognitiveservices.azure.com/.default";

    public static IChatClient Create(AgentSettings settings)
    {
        var options = new OpenAIClientOptions { Endpoint = settings.ClientEndpoint };

        var client = settings.Provider switch
        {
            // Entra ID sign-in goes through the SDK's token-policy constructor, which it still marks as
            // evaluation-only. It works against Foundry's OpenAI-compatible v1 endpoint.
#pragma warning disable OPENAI001
            AgentProvider.AzureFoundry when string.IsNullOrWhiteSpace(settings.ApiKey) =>
                new OpenAIClient(new BearerTokenPolicy(new DefaultAzureCredential(), CognitiveServicesScope), options),
#pragma warning restore OPENAI001
            AgentProvider.AzureFoundry => new OpenAIClient(new ApiKeyCredential(settings.ApiKey!), options),
            _ => new OpenAIClient(new ApiKeyCredential(string.IsNullOrWhiteSpace(settings.ApiKey) ? "not-used" : settings.ApiKey), options)
        };

        return client.GetChatClient(settings.Deployment).AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }
}

internal enum AgentProvider
{
    /// <summary>A deployment in Azure AI Foundry or Azure OpenAI, through its OpenAI-compatible v1 endpoint.</summary>
    AzureFoundry,

    /// <summary>Any OpenAI-compatible endpoint, for example Ollama on http://localhost:11434/v1/.</summary>
    OpenAICompatible
}

internal sealed record AgentSettings(AgentProvider Provider, string Endpoint, string Deployment, string? ApiKey)
{
    /// <summary>
    /// The endpoint the OpenAI client talks to: Foundry's resource endpoint gets its OpenAI-compatible
    /// <c>openai/v1/</c> path, other endpoints are used as given.
    /// </summary>
    public Uri ClientEndpoint => Provider == AgentProvider.AzureFoundry && !Endpoint.Contains("/openai/v1", StringComparison.OrdinalIgnoreCase)
        ? new Uri(Endpoint.TrimEnd('/') + "/openai/v1/")
        : new Uri(Endpoint);

    public static AgentSettings? From(IConfiguration configuration, out string problem)
    {
        var section = configuration.GetSection("Agent");
        var endpoint = section["Endpoint"];
        var deployment = section["Deployment"];

        if (!Enum.TryParse<AgentProvider>(section["Provider"] ?? nameof(AgentProvider.AzureFoundry), true, out var provider))
        {
            problem = $"Agent:Provider must be {nameof(AgentProvider.AzureFoundry)} or {nameof(AgentProvider.OpenAICompatible)}.";
            return null;
        }

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(deployment))
        {
            problem = "Agent:Endpoint and Agent:Deployment are not set.";
            return null;
        }

        problem = "";
        return new AgentSettings(provider, endpoint, deployment, section["ApiKey"]);
    }
}
