using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using MultiAgentApp.Configuration;
using OpenAI;
using System.ClientModel;

namespace MultiAgentApp.Agents;

/// <summary>
/// Creates <see cref="IChatClient"/> instances that back Microsoft Agent Framework agents.
///
/// Switching between Microsoft Foundry Local (local) and Microsoft Foundry (cloud) is
/// controlled solely by <see cref="AIOptions.UseFoundryLocal"/> in configuration.
/// No code changes are needed to flip between backends.
/// </summary>
public static class ChatClientFactory
{
    /// <summary>
    /// Creates an <see cref="IChatClient"/> based on the active AI backend configuration.
    /// </summary>
    /// <param name="options">AI provider options loaded from configuration.</param>
    /// <returns>A ready-to-use <see cref="IChatClient"/>.</returns>
    public static IChatClient CreateChatClient(AIOptions options) =>
        options.UseFoundryLocal
            ? CreateFoundryLocalClient(options.FoundryLocal)
            : CreateMicrosoftFoundryClient(options.MicrosoftFoundry);

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates an <see cref="IChatClient"/> backed by Microsoft Foundry Local.
    ///
    /// Foundry Local runs an OpenAI-compatible REST API locally (default port 5272).
    /// It does not require authentication; the <c>ApiKey</c> placeholder satisfies the
    /// client library validation without being sent to any server.
    ///
    /// Start Foundry Local and load a model before running the app:
    /// <code>
    ///   foundry model run phi-4-mini-reasoning
    /// </code>
    /// </summary>
    private static IChatClient CreateFoundryLocalClient(FoundryLocalOptions opts)
    {
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(opts.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(opts.Endpoint) });

        return openAiClient
            .GetChatClient(opts.ModelId)
            .AsIChatClient();
    }

    /// <summary>
    /// Creates an <see cref="IChatClient"/> backed by Microsoft Foundry.
    ///
    /// Populate <c>AI:MicrosoftFoundry:Endpoint</c>, <c>DeploymentName</c>, and <c>ProjectName</c>
    /// in configuration once a Microsoft Foundry resource is available.
    ///
    /// To switch to Microsoft Foundry:
    /// <list type="bullet">
    ///   <item>Set <c>AI:UseFoundryLocal = false</c> in configuration.</item>
    ///   <item>Populate <c>AI:MicrosoftFoundry:Endpoint</c>, <c>DeploymentName</c>, and <c>ProjectName</c>.</item>
    ///   <item>
    ///     Leave <c>AI:MicrosoftFoundry:ApiKey</c> empty to authenticate via
    ///     <see cref="DefaultAzureCredential"/> (Managed Identity / Azure CLI), or populate it
    ///     to use API key authentication.
    ///   </item>
    /// </list>
    /// </summary>
    private static IChatClient CreateMicrosoftFoundryClient(MicrosoftFoundryOptions opts)
    {
        if (string.IsNullOrWhiteSpace(opts.Endpoint))
        {
            throw new InvalidOperationException(
                "Microsoft Foundry endpoint is not configured. " +
                "Set AI:MicrosoftFoundry:Endpoint, DeploymentName, and ProjectName in configuration, " +
                "or set AI:UseFoundryLocal=true to use Microsoft Foundry Local instead.");
        }

        // When no API key is configured, use DefaultAzureCredential.
        // This is the preferred authentication method for Azure deployments and supports
        // Managed Identity, Azure CLI, environment variables, and other credential sources.
        if (string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            var azureClient = new AzureOpenAIClient(
                new Uri(opts.Endpoint),
                new DefaultAzureCredential());

            return azureClient
                .GetChatClient(opts.DeploymentName)
                .AsIChatClient();
        }

        // API key authentication – for local development or environments where a key is available.
        var keyedClient = new AzureOpenAIClient(
            new Uri(opts.Endpoint),
            new Azure.AzureKeyCredential(opts.ApiKey));

        return keyedClient
            .GetChatClient(opts.DeploymentName)
            .AsIChatClient();
    }
}
