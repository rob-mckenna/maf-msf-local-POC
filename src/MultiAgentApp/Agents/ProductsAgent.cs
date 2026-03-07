#pragma warning disable SKEXP0110 // Suppress experimental Semantic Kernel Agent Framework API warnings
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MultiAgentApp.Agents;

/// <summary>
/// Specialist agent that searches for and reasons about product catalog data.
/// Its tools come from the Products MCP server; callers should add the Products plugin
/// to the kernel before instantiating this agent.
/// </summary>
public sealed class ProductsAgent
{
    private const string AgentName = "ProductsAgent";

    private const string AgentInstructions = """
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

    private readonly Kernel _kernel;
    private readonly ILogger<ProductsAgent> _logger;

    public ProductsAgent(Kernel kernel, ILogger<ProductsAgent> logger)
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
