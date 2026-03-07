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

// ── Telemetry ────────────────────────────────────────────────────────────────

// Unique source name that correlates all agent traces in this run.
// Pass it to UseOpenTelemetry() on each agent to link spans across agents.
var telemetrySourceName = $"MultiAgentApp-{Guid.NewGuid():N}";

// Build the tracer provider (console + optional Azure Monitor Application Insights).
// To enable Application Insights: set Telemetry:ApplicationInsightsConnectionString.
using var tracerProvider = TelemetryConfiguration.BuildTracerProvider(telemetryOptions, telemetrySourceName);

// ── Dependency Injection ─────────────────────────────────────────────────────

var services = new ServiceCollection();
services.AddLogging(logging => logging.ConfigureLogLevel(telemetryOptions));
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
    : $"Microsoft Foundry  (deployment: {aiOptions.MicrosoftFoundry.DeploymentName}, project: {aiOptions.MicrosoftFoundry.ProjectName})";
var mcpBackend = mcpOptions.UseAzureApim ? "Azure API Management MCP" : "Local stdio MCP servers";
var appInsights = string.IsNullOrWhiteSpace(telemetryOptions.ApplicationInsightsConnectionString)
    ? "disabled (set Telemetry:ApplicationInsightsConnectionString to enable)"
    : "enabled";

logger.LogInformation("AI backend         : {Backend}", aiBackend);
logger.LogInformation("MCP backend        : {Backend}", mcpBackend);
logger.LogInformation("Application Insights: {Status}", appInsights);

// ── AI Client setup ───────────────────────────────────────────────────────────

logger.LogInformation("Creating AI chat client...");
var chatClient = ChatClientFactory.CreateChatClient(aiOptions);

// ── MCP Tool registration ────────────────────────────────────────────────────

await using var mcpFactory = new McpClientFactory(
    mcpOptions,
    loggerFactory.CreateLogger<McpClientFactory>());

logger.LogInformation("Connecting to MCP servers and loading tools...");
var weatherTools = await mcpFactory.GetWeatherToolsAsync();
var productsTools = await mcpFactory.GetProductsToolsAsync();
logger.LogInformation(
    "Loaded {WeatherCount} weather tools and {ProductCount} products tools.",
    weatherTools.Count, productsTools.Count);

// ── Agent setup ──────────────────────────────────────────────────────────────

// Each specialist agent gets its own chat client so tool sets are isolated.
// The orchestrator uses the shared client without tools.
var weatherAgent = new WeatherAgent(
    ChatClientFactory.CreateChatClient(aiOptions),
    weatherTools,
    loggerFactory);

var productsAgent = new ProductsAgent(
    ChatClientFactory.CreateChatClient(aiOptions),
    productsTools,
    loggerFactory);

var orchestrator = new OrchestratorAgent(
    chatClient,
    weatherAgent,
    productsAgent,
    loggerFactory,
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
