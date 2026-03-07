using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MultiAgentApp.Agents;
using MultiAgentApp.Configuration;

namespace MultiAgentApp.Tests.Agents;

public class ProductsAgentTests
{
    private static IChatClient CreateTestChatClient() =>
        ChatClientFactory.CreateChatClient(new AIOptions { UseFoundryLocal = true });

    [Fact]
    public void Build_ReturnsAIAgentWithCorrectName()
    {
        var client = CreateTestChatClient();
        var agent = new ProductsAgent(client, tools: []);

        var builtAgent = agent.Build();

        Assert.Equal("ProductsAgent", builtAgent.Name);
    }

    [Fact]
    public void Build_AgentHasDescription()
    {
        var client = CreateTestChatClient();
        var agent = new ProductsAgent(client, tools: []);

        var builtAgent = agent.Build();

        Assert.NotNull(builtAgent.Description);
        Assert.Contains("product", builtAgent.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_CalledMultipleTimes_ReturnsDistinctAgents()
    {
        var client = CreateTestChatClient();
        var agent = new ProductsAgent(client, tools: []);

        var agent1 = agent.Build();
        var agent2 = agent.Build();

        Assert.NotSame(agent1, agent2);
        Assert.Equal(agent1.Name, agent2.Name);
    }

    [Fact]
    public void Build_WithTools_AgentHasTools()
    {
        var client = CreateTestChatClient();
        AITool mockTool = AIFunctionFactory.Create(
            ([System.ComponentModel.Description("Product ID")] int id) => $"Product {id}",
            "get_product",
            "Gets a product");

        var agent = new ProductsAgent(client, tools: [mockTool]);

        var builtAgent = agent.Build();
        Assert.NotNull(builtAgent);
    }
}

