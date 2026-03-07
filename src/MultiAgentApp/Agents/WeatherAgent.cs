using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MultiAgentApp.Agents;

/// <summary>
/// Specialist agent that retrieves and reasons about weather data.
///
/// The agent receives its tools from the Weather MCP server (via <see cref="McpClientFactory"/>),
/// so it can call <c>get_current_weather</c> and <c>get_weather_forecast</c> without any
/// hard-coded weather logic.
/// </summary>
public sealed class WeatherAgent
{
    private const string Name = "WeatherAgent";
    private const string Description = "Specialist agent for weather conditions, forecasts, and climate queries.";

    private const string Instructions = """
        You are a weather specialist agent. Your job is to retrieve current weather conditions
        and forecasts for requested locations by calling the available weather tools.

        Always:
        - Use the get_current_weather tool to fetch real-time conditions.
        - Use the get_weather_forecast tool to fetch multi-day forecasts.
        - Format temperatures in both Celsius and Fahrenheit.
        - Mention notable weather alerts or hazards if present.
        - Keep responses concise and factual.

        Do not fabricate weather data; always use the provided tools.
        If no location is specified, ask the user to clarify.
        """;

    private readonly IChatClient _chatClient;
    private readonly IList<AITool> _tools;
    private readonly ILoggerFactory? _loggerFactory;

    /// <param name="chatClient">The underlying chat completion client (Foundry Local or Azure AI Foundry).</param>
    /// <param name="tools">MCP tool functions from the Weather MCP server.</param>
    /// <param name="loggerFactory">Optional logger factory for agent middleware.</param>
    public WeatherAgent(IChatClient chatClient, IList<AITool> tools, ILoggerFactory? loggerFactory = null)
    {
        _chatClient = chatClient;
        _tools = tools;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Builds and returns the underlying <see cref="AIAgent"/> ready for use in a workflow.
    /// </summary>
    public AIAgent Build() =>
        _chatClient.AsAIAgent(
            instructions: Instructions,
            name: Name,
            description: Description,
            tools: _tools,
            loggerFactory: _loggerFactory);
}


