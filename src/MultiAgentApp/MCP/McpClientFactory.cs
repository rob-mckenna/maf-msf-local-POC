using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using MultiAgentApp.Configuration;

namespace MultiAgentApp.MCP;

/// <summary>
/// Creates MCP (Model Context Protocol) clients and converts their tools to
/// <see cref="AITool"/> instances that Microsoft Agent Framework agents can call.
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
    /// Connects to the Weather MCP server and returns its tools as <see cref="AITool"/> objects.
    /// </summary>
    public async Task<IList<AITool>> GetWeatherToolsAsync(CancellationToken ct = default)
        => await GetToolsAsync(_options.WeatherServer, ct);

    /// <summary>
    /// Connects to the Products MCP server and returns its tools as <see cref="AITool"/> objects.
    /// </summary>
    public async Task<IList<AITool>> GetProductsToolsAsync(CancellationToken ct = default)
        => await GetToolsAsync(_options.ProductsServer, ct);

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<IList<AITool>> GetToolsAsync(McpServerOptions serverOptions, CancellationToken ct)
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

        // McpClientTool inherits from AIFunction which inherits from AITool,
        // so it can be used directly as AITool by MAF agents.
        return [.. tools];
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


