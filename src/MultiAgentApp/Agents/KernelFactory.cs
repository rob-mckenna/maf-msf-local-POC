using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using MultiAgentApp.Configuration;

namespace MultiAgentApp.Agents;

/// <summary>
/// Creates and configures <see cref="Kernel"/> instances.
///
/// The factory abstracts the AI backend selection so the rest of the application
/// never references vendor-specific APIs directly. Switching from Microsoft Foundry Local
/// to Azure AI Foundry requires only a config change (<c>AI:UseFoundryLocal = false</c>).
/// </summary>
public static class KernelFactory
{
    /// <summary>
    /// Builds a <see cref="Kernel"/> wired to either Foundry Local or Azure AI Foundry
    /// based on <paramref name="options"/>.
    /// </summary>
    public static Kernel CreateKernel(AIOptions options, ILoggerFactory loggerFactory)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(loggerFactory);

        if (options.UseFoundryLocal)
        {
            RegisterFoundryLocal(builder, options.FoundryLocal);
        }
        else
        {
            RegisterAzureAIFoundry(builder, options.AzureAIFoundry);
        }

        return builder.Build();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Registers a Foundry Local chat completion service.
    /// Foundry Local exposes an OpenAI-compatible API, so the standard SK OpenAI
    /// connector is used with a custom endpoint URI.
    /// </summary>
    private static void RegisterFoundryLocal(IKernelBuilder builder, FoundryLocalOptions opts)
    {
        builder.AddOpenAIChatCompletion(
            modelId: opts.ModelId,
            endpoint: new Uri(opts.Endpoint),
            apiKey: opts.ApiKey);
    }

    /// <summary>
    /// Registers an Azure AI Foundry chat completion service.
    /// Uncomment the Azure OpenAI connector once the cloud environment is available.
    /// </summary>
    private static void RegisterAzureAIFoundry(IKernelBuilder builder, AzureAIFoundryOptions opts)
    {
        // ── Azure AI Foundry ────────────────────────────────────────────────────
        // Uncomment once Azure AI Foundry is available:
        //
        // builder.AddAzureOpenAIChatCompletion(
        //     deploymentName: opts.DeploymentName,
        //     endpoint: opts.Endpoint,
        //     apiKey: opts.ApiKey);
        // ── End Azure AI Foundry ─────────────────────────────────────────────────

        // Fallback for development: use Foundry Local even when UseFoundryLocal=false
        // until a real Azure endpoint is configured.
        if (string.IsNullOrWhiteSpace(opts.Endpoint))
        {
            throw new InvalidOperationException(
                "Azure AI Foundry endpoint is not configured. " +
                "Set AI:AzureAIFoundry:Endpoint, DeploymentName, and ApiKey in configuration, " +
                "or set AI:UseFoundryLocal=true to use Microsoft Foundry Local instead.");
        }

        builder.AddOpenAIChatCompletion(
            modelId: opts.DeploymentName,
            endpoint: new Uri(opts.Endpoint),
            apiKey: opts.ApiKey);
    }
}
