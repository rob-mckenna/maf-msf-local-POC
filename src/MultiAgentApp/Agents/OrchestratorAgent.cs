using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text;

namespace MultiAgentApp.Agents;

/// <summary>
/// Orchestrates specialist nodes through a state-graph style flow.
/// </summary>
public sealed class OrchestratorAgent
{
    private const string Name = "OrchestratorAgent";

    private const string SynthesisInstructions = """
        You are an orchestrator that combines specialist outputs into one final response.
        Provide a concise, clear answer and avoid repeating the same points.
        If either specialist reports uncertainty, preserve that uncertainty in the final answer.
        """;

    private readonly IChatClient _chatClient;
    private readonly WeatherAgent _weatherAgent;
    private readonly ProductsAgent _productsAgent;
    private readonly ILogger<OrchestratorAgent> _logger;

    public OrchestratorAgent(
        IChatClient chatClient,
        WeatherAgent weatherAgent,
        ProductsAgent productsAgent,
        ILoggerFactory? _,
        ILogger<OrchestratorAgent> logger)
    {
        _chatClient = chatClient;
        _weatherAgent = weatherAgent;
        _productsAgent = productsAgent;
        _logger = logger;
    }

    public async Task<string> RunAsync(string userMessage, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting orchestration graph for: {Message}", userMessage);

        var state = new GraphState(userMessage);

        // Router node
        Route(state);

        // Specialist nodes
        if (state.NeedsWeather)
        {
            state.WeatherResponse = await _weatherAgent.RunAsync(userMessage, ct);
        }

        if (state.NeedsProducts)
        {
            state.ProductsResponse = await _productsAgent.RunAsync(userMessage, ct);
        }

        // Synthesis node
        var finalResponse = await SynthesizeAsync(state, ct);
        _logger.LogInformation("Orchestration graph complete.");

        return finalResponse;
    }

    private static void Route(GraphState state)
    {
        var text = state.UserMessage.ToLowerInvariant();
        var weatherTerms = new[] { "weather", "forecast", "temperature", "rain", "snow", "climate" };
        var productTerms = new[] { "product", "inventory", "stock", "price", "buy", "catalog", "gear", "jacket" };

        state.NeedsWeather = weatherTerms.Any(text.Contains);
        state.NeedsProducts = productTerms.Any(text.Contains);

        // Default to both for broad/ambiguous requests.
        if (!state.NeedsWeather && !state.NeedsProducts)
        {
            state.NeedsWeather = true;
            state.NeedsProducts = true;
        }
    }

    private async Task<string> SynthesizeAsync(GraphState state, CancellationToken ct)
    {
        if (!state.NeedsWeather && !state.NeedsProducts)
        {
            return "I couldn't determine which specialists to route to.";
        }

        var specialistSummaries = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(state.WeatherResponse))
        {
            specialistSummaries.AppendLine($"[{_weatherAgent.Name}] {state.WeatherResponse}");
        }

        if (!string.IsNullOrWhiteSpace(state.ProductsResponse))
        {
            specialistSummaries.AppendLine($"[{_productsAgent.Name}] {state.ProductsResponse}");
        }

        if (specialistSummaries.Length == 0)
        {
            return "The workflow completed but no specialist output was generated.";
        }

        var response = await _chatClient.GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, SynthesisInstructions),
                new ChatMessage(ChatRole.User, $"Original user question: {state.UserMessage}"),
                new ChatMessage(ChatRole.User, $"Specialist outputs:\n{specialistSummaries}")
            ],
            cancellationToken: ct);

        var text = response.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return specialistSummaries.ToString().Trim();
        }

        return text;
    }

    private sealed class GraphState(string userMessage)
    {
        public string UserMessage { get; } = userMessage;
        public bool NeedsWeather { get; set; }
        public bool NeedsProducts { get; set; }
        public string WeatherResponse { get; set; } = string.Empty;
        public string ProductsResponse { get; set; } = string.Empty;
    }
}
