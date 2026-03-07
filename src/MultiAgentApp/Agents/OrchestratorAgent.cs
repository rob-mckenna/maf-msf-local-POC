#pragma warning disable SKEXP0110 // Suppress experimental Semantic Kernel Agent Framework API warnings
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MultiAgentApp.Agents;

/// <summary>
/// Orchestrates a multi-agent workflow by coordinating <see cref="WeatherAgent"/>
/// and <see cref="ProductsAgent"/> through Semantic Kernel's <see cref="AgentGroupChat"/>.
///
/// Workflow:
///   1. A user question is submitted.
///   2. The orchestrator routes the question to the appropriate specialist agent(s).
///   3. Specialist agents call their respective MCP server tools to fetch real data.
///   4. The orchestrator synthesises a final, coherent answer.
/// </summary>
public sealed class OrchestratorAgent
{
    private const string OrchestratorName = "OrchestratorAgent";

    private const string OrchestratorInstructions = """
        You are the orchestrating agent in a multi-agent system. Your responsibilities:

        1. Understand the user's request.
        2. Determine which specialist agents are needed:
           - WeatherAgent   → weather conditions, forecasts, climate questions
           - ProductsAgent  → product search, inventory, pricing questions
        3. Delegate tasks to the appropriate specialist(s).
        4. Wait for the specialists' responses.
        5. Combine their outputs into a single, clear, and helpful reply to the user.

        When you have collected all necessary information and are ready to give the
        final answer to the user, start your message with the word "FINISHED".
        """;

    private readonly Kernel _kernel;
    private readonly WeatherAgent _weatherAgent;
    private readonly ProductsAgent _productsAgent;
    private readonly ILogger<OrchestratorAgent> _logger;

    public OrchestratorAgent(
        Kernel kernel,
        WeatherAgent weatherAgent,
        ProductsAgent productsAgent,
        ILogger<OrchestratorAgent> logger)
    {
        _kernel = kernel;
        _weatherAgent = weatherAgent;
        _productsAgent = productsAgent;
        _logger = logger;
    }

    /// <summary>
    /// Runs the multi-agent workflow for the given <paramref name="userMessage"/> and
    /// returns the orchestrator's final synthesised response.
    /// </summary>
    public async Task<string> RunAsync(string userMessage, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting multi-agent workflow for: {Message}", userMessage);

        var orchestrator = BuildOrchestratorAgent();
        var weatherAgent = _weatherAgent.Build();
        var productsAgent = _productsAgent.Build();

        var terminationFunction = KernelFunctionFactory.CreateFromPrompt("""
            Determine whether the orchestrator has finished by examining its last message.
            The orchestrator signals completion by starting its message with the word "FINISHED".

            Last orchestrator message:
            {{$lastmessage}}

            Reply with only "yes" if the orchestrator is done, or "no" otherwise.
            """);

        var chat = new AgentGroupChat(orchestrator, weatherAgent, productsAgent)
        {
            ExecutionSettings = new AgentGroupChatSettings
            {
                TerminationStrategy = new KernelFunctionTerminationStrategy(
                    terminationFunction,
                    _kernel)
                {
                    Agents = [orchestrator],
                    ResultParser = result =>
                        result.GetValue<string>()?.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                    HistoryVariableName = "lastmessage",
                    MaximumIterations = 10
                },
                SelectionStrategy = new SequentialSelectionStrategy()
            }
        };

        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userMessage));

        var responseBuilder = new System.Text.StringBuilder();

        await foreach (var response in chat.InvokeAsync(ct))
        {
            _logger.LogDebug("[{Agent}]: {Content}", response.AuthorName, response.Content);

            if (response.AuthorName == OrchestratorName && response.Content != null)
            {
                var content = response.Content.Replace("FINISHED", string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    responseBuilder.AppendLine(content);
                }
            }
        }

        var finalResponse = responseBuilder.ToString().Trim();

        if (string.IsNullOrWhiteSpace(finalResponse))
        {
            finalResponse = "The agents completed the workflow but produced no final response.";
        }

        _logger.LogInformation("Multi-agent workflow complete.");
        return finalResponse;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private ChatCompletionAgent BuildOrchestratorAgent() =>
        new()
        {
            Name = OrchestratorName,
            Instructions = OrchestratorInstructions,
            Kernel = _kernel.Clone(),
            Arguments = new KernelArguments(new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };
}
