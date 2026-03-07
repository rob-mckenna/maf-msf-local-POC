namespace MultiAgentApp.Configuration;

/// <summary>
/// Top-level AI provider options.
/// Toggle <see cref="UseFoundryLocal"/> to switch between Microsoft Foundry Local
/// (running on the developer's machine) and Microsoft Foundry (cloud).
/// </summary>
public sealed class AIOptions
{
    public const string SectionName = "AI";

    /// <summary>
    /// When <c>true</c>, the app connects to a locally-running Microsoft Foundry Local instance.
    /// Set to <c>false</c> to use Microsoft Foundry once a cloud environment is available.
    /// </summary>
    public bool UseFoundryLocal { get; set; } = true;

    /// <summary>Connection settings for Microsoft Foundry Local (local inference).</summary>
    public FoundryLocalOptions FoundryLocal { get; set; } = new();

    /// <summary>Connection settings for Microsoft Foundry (cloud). Used when <see cref="UseFoundryLocal"/> is <c>false</c>.</summary>
    public MicrosoftFoundryOptions MicrosoftFoundry { get; set; } = new();
}

/// <summary>
/// Connection settings for Microsoft Foundry Local.
/// Foundry Local exposes an OpenAI-compatible REST API, typically at http://localhost:5272/v1.
/// </summary>
public sealed class FoundryLocalOptions
{
    /// <summary>Base URL of the Foundry Local OpenAI-compatible endpoint.</summary>
    public string Endpoint { get; set; } = "http://localhost:5272/v1";

    /// <summary>
    /// Name of the model loaded in Foundry Local.
    /// List available models with: <c>foundry model list</c>.
    /// </summary>
    public string ModelId { get; set; } = "phi-4-mini-reasoning";

    /// <summary>
    /// Placeholder API key value (Foundry Local does not require authentication,
    /// but the OpenAI client library requires a non-empty value).
    /// </summary>
    public string ApiKey { get; set; } = "foundry-local";
}

/// <summary>
/// Connection settings for Microsoft Foundry (cloud).
/// Populated from environment variables or Azure Key Vault once a cloud environment is available.
/// </summary>
public sealed class MicrosoftFoundryOptions
{
    /// <summary>Microsoft Foundry endpoint URL.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Chat completion deployment name in Microsoft Foundry.</summary>
    public string DeploymentName { get; set; } = string.Empty;

    /// <summary>
    /// Microsoft Foundry project name.
    /// Used to scope requests to a specific project within the Microsoft Foundry workspace.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI API key (optional).
    /// When empty, the application authenticates using <c>DefaultAzureCredential</c>
    /// (Managed Identity, Azure CLI, environment variables, etc.) – the preferred approach
    /// for Azure deployments. When set, API key authentication is used instead.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
