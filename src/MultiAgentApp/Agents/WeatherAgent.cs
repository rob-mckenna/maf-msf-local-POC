#pragma warning disable SKEXP0110 // Suppress experimental Semantic Kernel Agent Framework API warnings
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MultiAgentApp.Agents;

/// <summary>
/// Specialist agent that retrieves and reasons about weather data.
/// Its tools come from the Weather MCP server; callers should add the Weather plugin
/// to the kernel before instantiating this agent.
/// </summary>
public sealed class WeatherAgent
{
    private const string AgentName = "WeatherAgent";

    private const string AgentInstructions = """
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

    private readonly Kernel _kernel;
    private readonly ILogger<WeatherAgent> _logger;

    public WeatherAgent(Kernel kernel, ILogger<WeatherAgent> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    /// <summary>Builds and returns the underlying <see cref="ChatCompletionAgent"/>.</summary>
    public ChatCompletionAgent Build()
    {
        _logger.LogDebug("Building {AgentName}.", AgentName);

        return new ChatCompletionAgent
        {
            Name = AgentName,
            Instructions = AgentInstructions,
            Kernel = _kernel.Clone(),
            Arguments = new KernelArguments(new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };
    }
}
