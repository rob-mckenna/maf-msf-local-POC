using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MultiAgentApp.Agents;

/// <summary>
/// Specialist agent that searches for and reasons about product catalog data.
///
/// The agent receives its tools from the Products MCP server (via <see cref="McpClientFactory"/>),
/// so it can call <c>search_products</c>, <c>get_product_details</c>, and <c>check_inventory</c>
/// without any hard-coded catalog logic.
/// </summary>
public sealed class ProductsAgent
{
    private const string Name = "ProductsAgent";
    private const string Description = "Specialist agent for product catalog searches, details, and inventory queries.";

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
    private readonly ILoggerFactory? _loggerFactory;

    /// <param name="chatClient">The underlying chat completion client (Foundry Local or Microsoft Foundry).</param>
    /// <param name="tools">MCP tool functions from the Products MCP server.</param>
    /// <param name="loggerFactory">Optional logger factory for agent middleware.</param>
    public ProductsAgent(IChatClient chatClient, IList<AITool> tools, ILoggerFactory? loggerFactory = null)
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

