using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MultiAgentApp.Agents;
using MultiAgentApp.Configuration;
using MultiAgentApp.MCP;
using MultiAgentApp.Telemetry;

// ── Configuration ────────────────────────────────────────────────────────────

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var aiOptions = configuration.GetSection(AIOptions.SectionName).Get<AIOptions>() ?? new AIOptions();
var mcpOptions = configuration.GetSection(McpOptions.SectionName).Get<McpOptions>() ?? new McpOptions();
var telemetryOptions = configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>() ?? new TelemetryOptions();

// ── Dependency Injection ─────────────────────────────────────────────────────

var services = new ServiceCollection();

services.AddLogging(logging => logging.ConfigureTelemetry(telemetryOptions));
services.AddApplicationInsightsTelemetry(telemetryOptions);

services.AddSingleton(aiOptions);
services.AddSingleton(mcpOptions);
services.AddSingleton(telemetryOptions);

var serviceProvider = services.BuildServiceProvider();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("MultiAgentApp");

// ── Startup Banner ───────────────────────────────────────────────────────────

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║      Multi-Agent App  –  Microsoft Agent Framework       ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
Console.ResetColor();

var aiBackend = aiOptions.UseFoundryLocal
    ? $"Microsoft Foundry Local  ({aiOptions.FoundryLocal.ModelId}  @ {aiOptions.FoundryLocal.Endpoint})"
    : $"Azure AI Foundry  (deployment: {aiOptions.AzureAIFoundry.DeploymentName})";

var mcpBackend = mcpOptions.UseAzureApim ? "Azure API Management MCP" : "Local stdio MCP servers";

logger.LogInformation("AI backend  : {Backend}", aiBackend);
logger.LogInformation("MCP backend : {Backend}", mcpBackend);

// ── Kernel setup ─────────────────────────────────────────────────────────────

logger.LogInformation("Building Semantic Kernel...");
var kernel = KernelFactory.CreateKernel(aiOptions, loggerFactory);

// ── MCP Plugin registration ──────────────────────────────────────────────────

await using var mcpFactory = new McpClientFactory(
    mcpOptions,
    loggerFactory.CreateLogger<McpClientFactory>());

logger.LogInformation("Connecting to MCP servers and registering plugins...");

// Clone kernels for each agent so plugins are isolated
var weatherKernel = kernel.Clone();
var productsKernel = kernel.Clone();

await mcpFactory.AddWeatherPluginToKernelAsync(weatherKernel);
await mcpFactory.AddProductsPluginToKernelAsync(productsKernel);

// ── Agent setup ──────────────────────────────────────────────────────────────

var weatherAgent = new WeatherAgent(
    weatherKernel,
    loggerFactory.CreateLogger<WeatherAgent>());

var productsAgent = new ProductsAgent(
    productsKernel,
    loggerFactory.CreateLogger<ProductsAgent>());

var orchestrator = new OrchestratorAgent(
    kernel,
    weatherAgent,
    productsAgent,
    loggerFactory.CreateLogger<OrchestratorAgent>());

// ── Demo Queries ─────────────────────────────────────────────────────────────

var demoQueries = new[]
{
    "What is the current weather in Seattle and do you have any waterproof jackets in stock?",
    "I'm planning a camping trip to Denver next weekend. What will the weather be like, " +
        "and what camping gear do you have available?",
    "Show me the 5-day forecast for New York City."
};

logger.LogInformation("Running {Count} demo queries through the multi-agent workflow.", demoQueries.Length);

foreach (var (query, index) in demoQueries.Select((q, i) => (q, i + 1)))
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"── Query {index} of {demoQueries.Length} ──────────────────────────────────────────────────");
    Console.ResetColor();
    Console.WriteLine($"User: {query}");
    Console.WriteLine();

    try
    {
        var response = await orchestrator.RunAsync(query);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Assistant:");
        Console.ResetColor();
        Console.WriteLine(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Query {Index} failed.", index);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {ex.Message}");
        Console.ResetColor();
    }
}

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║                     Demo Complete                        ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
Console.ResetColor();
