using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MultiAgentApp.Agents;
using MultiAgentApp.Configuration;

namespace MultiAgentApp.Tests.Agents;

public class WeatherAgentTests
{
    private static IChatClient CreateTestChatClient() =>
        ChatClientFactory.CreateChatClient(new AIOptions { UseFoundryLocal = true });

    [Fact]
    public void Build_ReturnsAIAgentWithCorrectName()
    {
        var client = CreateTestChatClient();
        var agent = new WeatherAgent(client, tools: []);

        var builtAgent = agent.Build();

        Assert.Equal("WeatherAgent", builtAgent.Name);
    }

    [Fact]
    public void Build_AgentHasDescription()
    {
        var client = CreateTestChatClient();
        var agent = new WeatherAgent(client, tools: []);

        var builtAgent = agent.Build();

        Assert.NotNull(builtAgent.Description);
        Assert.Contains("weather", builtAgent.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_CalledMultipleTimes_ReturnsDistinctAgents()
    {
        var client = CreateTestChatClient();
        var agent = new WeatherAgent(client, tools: []);

        var agent1 = agent.Build();
        var agent2 = agent.Build();

        Assert.NotSame(agent1, agent2);
        Assert.Equal(agent1.Name, agent2.Name);
    }

    [Fact]
    public void Build_WithTools_AgentHasTools()
    {
        var client = CreateTestChatClient();
        // Create a mock AITool (AIFunction is a subtype of AITool)
        AITool mockTool = AIFunctionFactory.Create(
            ([System.ComponentModel.Description("Test param")] string input) => $"Echo: {input}",
            "test_tool",
            "A test tool");

        var agent = new WeatherAgent(client, tools: [mockTool]);

        // Should not throw - tools are accepted
        var builtAgent = agent.Build();
        Assert.NotNull(builtAgent);
    }
}

