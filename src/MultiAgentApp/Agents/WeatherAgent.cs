using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MultiAgentApp.Agents;

/// <summary>
/// Weather specialist node used by the orchestrator graph.
/// </summary>
public sealed class WeatherAgent
{
    public const string NodeName = "WeatherAgent";
    public const string NodeDescription = "Specialist node for weather conditions, forecasts, and climate queries.";

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
    private readonly ILogger<WeatherAgent>? _logger;

    public WeatherAgent(IChatClient chatClient, IList<AITool> tools, ILoggerFactory? loggerFactory = null)
    {
        _chatClient = chatClient;
        _tools = tools;
        _logger = loggerFactory?.CreateLogger<WeatherAgent>();
    }

    public string Name => NodeName;
    public string Description => NodeDescription;

    public async Task<string> RunAsync(string userMessage, CancellationToken ct = default)
    {
        var response = await _chatClient.GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, Instructions),
                new ChatMessage(ChatRole.User, userMessage)
            ],
            new ChatOptions { Tools = _tools },
            ct);

        var text = response.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger?.LogWarning("{Agent} returned an empty response.", Name);
            return "I couldn't retrieve weather details from the available tools.";
        }

        return text;
    }
}
