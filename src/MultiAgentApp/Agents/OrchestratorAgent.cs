using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MultiAgentApp.Agents;

/// <summary>
/// Orchestrates a multi-agent workflow by coordinating <see cref="WeatherAgent"/>
/// and <see cref="ProductsAgent"/> using Microsoft Agent Framework's
/// <see cref="AgentWorkflowBuilder"/> handoff pattern.
///
/// Workflow:
///   1. The user message is sent to the OrchestratorAgent.
///   2. The orchestrator decides which specialist agent(s) to hand off to.
///   3. Specialist agents call their MCP server tools to fetch real data.
///   4. Specialists hand back to the orchestrator, which synthesises a final answer.
/// </summary>
public sealed class OrchestratorAgent
{
    private const string Name = "OrchestratorAgent";
    private const string Description = "Orchestrates weather and product queries by routing to specialist agents.";

    private const string Instructions = """
        You are the orchestrating agent in a multi-agent system. Your responsibilities:

        1. Understand the user's request.
        2. Determine which specialist agents are needed and hand off to them:
           - WeatherAgent   → weather conditions, forecasts, climate questions
           - ProductsAgent  → product search, inventory, pricing questions
        3. When both types of information are needed, hand off to each specialist in turn.
        4. After receiving responses from all required specialists, synthesise them into a
           single clear, concise, and helpful reply to the user.

        Do NOT answer weather or product questions directly—always hand off to the correct
        specialist agent first. Only provide the final synthesised answer yourself.
        """;

    private readonly IChatClient _chatClient;
    private readonly WeatherAgent _weatherAgent;
    private readonly ProductsAgent _productsAgent;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<OrchestratorAgent> _logger;

    public OrchestratorAgent(
        IChatClient chatClient,
        WeatherAgent weatherAgent,
        ProductsAgent productsAgent,
        ILoggerFactory? loggerFactory,
        ILogger<OrchestratorAgent> logger)
    {
        _chatClient = chatClient;
        _weatherAgent = weatherAgent;
        _productsAgent = productsAgent;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Runs the multi-agent handoff workflow for the given <paramref name="userMessage"/>
    /// and returns the orchestrator's final synthesised response.
    /// </summary>
    public async Task<string> RunAsync(string userMessage, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting multi-agent workflow for: {Message}", userMessage);

        var orchestrator = _chatClient.AsAIAgent(
            instructions: Instructions,
            name: Name,
            description: Description,
            loggerFactory: _loggerFactory);

        var weatherAgent = _weatherAgent.Build();
        var productsAgent = _productsAgent.Build();

        // ── Build handoff workflow ────────────────────────────────────────────
        // The orchestrator can hand off to either specialist and receive control back.
        // The workflow ends when the orchestrator produces a final response without
        // handing off further.
        var workflow = AgentWorkflowBuilder
            .CreateHandoffBuilderWith(orchestrator)
            .WithHandoffs(orchestrator, [weatherAgent, productsAgent])
            .WithHandoffs([weatherAgent, productsAgent], orchestrator)
            .Build();

        // ── Execute the workflow ──────────────────────────────────────────────
        var responseBuilder = new System.Text.StringBuilder();

        await using var run = await InProcessExecution.RunStreamingAsync(
            workflow, userMessage, cancellationToken: ct);

        // Send the turn token to start agent processing
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (var evt in run.WatchStreamAsync(ct))
        {
            switch (evt)
            {
                case AgentResponseUpdateEvent update:
                    _logger.LogDebug("[{Executor}]: {Text}",
                        update.ExecutorId, update.Update.Text);
                    break;

                case WorkflowOutputEvent output:
                    // The workflow output is the final response from the orchestrator
                    var messages = output.As<List<ChatMessage>>();
                    if (messages?.Count > 0)
                    {
                        var lastMessage = messages[messages.Count - 1];
                        responseBuilder.Append(lastMessage.Text);
                    }
                    break;
            }
        }

        // If no structured output was produced, collect agent responses directly
        if (responseBuilder.Length == 0)
        {
            _logger.LogWarning("No WorkflowOutputEvent received; collecting agent responses.");
            await using var fallbackRun = await InProcessExecution.RunStreamingAsync(
                workflow, userMessage, cancellationToken: ct);
            await fallbackRun.TrySendMessageAsync(new TurnToken(emitEvents: true));

            await foreach (var evt in fallbackRun.WatchStreamAsync(ct))
            {
                if (evt is AgentResponseUpdateEvent update
                    && update.ExecutorId == Name
                    && !string.IsNullOrEmpty(update.Update.Text))
                {
                    responseBuilder.Append(update.Update.Text);
                }
            }
        }

        var finalResponse = responseBuilder.ToString().Trim();

        if (string.IsNullOrWhiteSpace(finalResponse))
        {
            finalResponse = "The agents completed the workflow but produced no final response. " +
                            "Ensure the AI backend is reachable and the model is loaded.";
        }

        _logger.LogInformation("Multi-agent workflow complete.");
        return finalResponse;
    }
}

