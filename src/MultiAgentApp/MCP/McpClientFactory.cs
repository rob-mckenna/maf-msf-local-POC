using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using MultiAgentApp.Configuration;

namespace MultiAgentApp.MCP;

/// <summary>
/// Creates MCP (Model Context Protocol) clients and registers their tools as Semantic Kernel plugins.
///
/// Transport strategy:
/// • Local (default): launches server executables as child processes via stdio transport.
/// • Azure APIM: switches to an HTTP SSE transport once Azure API Management MCP endpoints
///   become available. Toggle <c>MCP:UseAzureApim</c> in configuration.
/// </summary>
public sealed class McpClientFactory : IAsyncDisposable
{
    private readonly McpOptions _options;
    private readonly ILogger<McpClientFactory> _logger;
    private readonly List<McpClient> _clients = [];

    public McpClientFactory(McpOptions options, ILogger<McpClientFactory> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Connects to the Weather MCP server and retrieves its tool list.
    /// </summary>
    public async Task<IList<McpClientTool>> GetWeatherToolsAsync(CancellationToken ct = default)
        => await GetToolsAsync(_options.WeatherServer, ct);

    /// <summary>
    /// Connects to the Products MCP server and retrieves its tool list.
    /// </summary>
    public async Task<IList<McpClientTool>> GetProductsToolsAsync(CancellationToken ct = default)
        => await GetToolsAsync(_options.ProductsServer, ct);

    /// <summary>
    /// Adds the Weather MCP tools to the given <paramref name="kernel"/> as a plugin named "Weather".
    /// </summary>
    public async Task AddWeatherPluginToKernelAsync(Kernel kernel, CancellationToken ct = default)
    {
        var tools = await GetWeatherToolsAsync(ct);
        kernel.Plugins.AddFromFunctions("Weather", tools.Select(t => t.AsKernelFunction()));
        _logger.LogInformation("Registered {Count} Weather tools as kernel plugin.", tools.Count);
    }

    /// <summary>
    /// Adds the Products MCP tools to the given <paramref name="kernel"/> as a plugin named "Products".
    /// </summary>
    public async Task AddProductsPluginToKernelAsync(Kernel kernel, CancellationToken ct = default)
    {
        var tools = await GetProductsToolsAsync(ct);
        kernel.Plugins.AddFromFunctions("Products", tools.Select(t => t.AsKernelFunction()));
        _logger.LogInformation("Registered {Count} Products tools as kernel plugin.", tools.Count);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<IList<McpClientTool>> GetToolsAsync(McpServerOptions serverOptions, CancellationToken ct)
    {
        IClientTransport transport = _options.UseAzureApim
            ? BuildApimTransport(serverOptions)
            : BuildStdioTransport(serverOptions);

        _logger.LogInformation(
            "Connecting to MCP server '{Name}' via {Mode} transport.",
            serverOptions.Name,
            _options.UseAzureApim ? "APIM" : "stdio");

        var client = await McpClient.CreateAsync(transport, cancellationToken: ct);
        _clients.Add(client);

        var tools = await client.ListToolsAsync(cancellationToken: ct);
        return tools;
    }

    /// <summary>
    /// Creates a stdio transport that launches the server as a child process.
    /// Used during local development and CI.
    /// </summary>
    private static IClientTransport BuildStdioTransport(McpServerOptions opts) =>
        new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = opts.Name,
            Command = opts.StdioCommand,
            Arguments = opts.StdioArguments
        });

    /// <summary>
    /// Creates an HTTP SSE transport that connects to Azure API Management.
    /// Replace this stub with the actual APIM SSE URL once available.
    /// </summary>
    private static IClientTransport BuildApimTransport(McpServerOptions opts)
    {
        // ── Azure APIM MCP Transport ────────────────────────────────────────────
        // Uncomment once Azure API Management MCP support is available:
        //
        // return new SseClientTransport(new SseClientTransportOptions
        // {
        //     Endpoint = new Uri(opts.ApimEndpoint),
        //     AdditionalHeaders = new Dictionary<string, string>
        //     {
        //         ["Ocp-Apim-Subscription-Key"] = opts.ApimApiKey
        //     }
        // });
        // ── End Azure APIM MCP Transport ────────────────────────────────────────

        throw new NotImplementedException(
            $"Azure APIM transport is not yet implemented for '{opts.Name}'. " +
            "Set MCP:UseAzureApim=false to continue using local stdio transport.");
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
        {
            await client.DisposeAsync();
        }
        _clients.Clear();
    }
}

