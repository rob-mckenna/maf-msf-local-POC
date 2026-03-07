namespace MultiAgentApp.Configuration;

/// <summary>
/// Options for MCP (Model Context Protocol) server connections.
/// Local stdio-based servers are used during development.  When Azure API Management MCP support
/// becomes available, set <see cref="UseAzureApim"/> to <c>true</c> and populate the APIM options.
/// </summary>
public sealed class McpOptions
{
    public const string SectionName = "MCP";

    /// <summary>
    /// When <c>true</c>, the app connects to MCP servers hosted in Azure API Management
    /// instead of launching local stdio processes.
    /// </summary>
    public bool UseAzureApim { get; set; } = false;

    /// <summary>Settings for the Weather MCP server.</summary>
    public McpServerOptions WeatherServer { get; set; } = new()
    {
        Name = "WeatherMcpServer",
        StdioCommand = "dotnet",
        StdioArguments = ["run", "--project", "src/WeatherMcpServer/WeatherMcpServer.csproj", "--no-build"],
        ApimEndpoint = string.Empty
    };

    /// <summary>Settings for the Products MCP server.</summary>
    public McpServerOptions ProductsServer { get; set; } = new()
    {
        Name = "ProductsMcpServer",
        StdioCommand = "dotnet",
        StdioArguments = ["run", "--project", "src/ProductsMcpServer/ProductsMcpServer.csproj", "--no-build"],
        ApimEndpoint = string.Empty
    };
}

/// <summary>Per-server MCP connection settings.</summary>
public sealed class McpServerOptions
{
    /// <summary>Friendly name used when registering the plugin in the Semantic Kernel.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Executable to launch for stdio transport (e.g. "dotnet").</summary>
    public string StdioCommand { get; set; } = string.Empty;

    /// <summary>Arguments passed to <see cref="StdioCommand"/> for stdio transport.</summary>
    public string[] StdioArguments { get; set; } = [];

    /// <summary>Azure APIM MCP endpoint URL (used when <see cref="McpOptions.UseAzureApim"/> is <c>true</c>).</summary>
    public string ApimEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Subscription key or bearer token for the APIM endpoint.
    /// In production, retrieve from Azure Key Vault or Managed Identity.
    /// </summary>
    public string ApimApiKey { get; set; } = string.Empty;
}
