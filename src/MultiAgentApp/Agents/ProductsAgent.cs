using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MultiAgentApp.Agents;

/// <summary>
/// Products specialist node used by the orchestrator graph.
/// </summary>
public sealed class ProductsAgent
{
    public const string NodeName = "ProductsAgent";
    public const string NodeDescription = "Specialist node for product catalog searches, details, and inventory queries.";

    private const string Instructions = """
        You are a product catalog specialist agent. Your job is to search for products,
        retrieve product details, and check inventory availability by calling the available
        product tools.

        Always:
        - Use the search_products tool when looking for products by name or category.
        - Use the get_product_details tool to fetch a specific product's full information.
        - Use the check_inventory tool to verify stock availability.
        - Present prices clearly with currency symbols.
        - Highlight key product features and specifications.

        Do not fabricate product data; always use the provided tools.
        If the product cannot be found, say so clearly and suggest alternatives where possible.
        """;

    private readonly IChatClient _chatClient;
    private readonly IList<AITool> _tools;
    private readonly ILogger<ProductsAgent>? _logger;

    public ProductsAgent(IChatClient chatClient, IList<AITool> tools, ILoggerFactory? loggerFactory = null)
    {
        _chatClient = chatClient;
        _tools = tools;
        _logger = loggerFactory?.CreateLogger<ProductsAgent>();
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
            return "I couldn't retrieve product details from the available tools.";
        }

        return text;
    }
}
