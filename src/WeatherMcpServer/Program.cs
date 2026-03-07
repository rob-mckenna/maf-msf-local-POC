using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeatherMcpServer.Tools;

// ── Weather MCP Server ───────────────────────────────────────────────────────
// This server exposes mocked weather data via the Model Context Protocol (MCP).
//
// Transport: stdio (default for local development).
// When Azure API Management MCP support is available, this server can be deployed
// as an HTTP/SSE endpoint and registered behind an APIM policy — no code changes
// needed; the client switches transport via the MCP:UseAzureApim configuration flag.

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.FormatterName = "simple");
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var host = builder.Build();

await host.RunAsync();
