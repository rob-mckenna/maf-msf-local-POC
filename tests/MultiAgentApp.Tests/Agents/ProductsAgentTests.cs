#pragma warning disable SKEXP0110 // Suppress experimental Semantic Kernel Agent Framework API warnings
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using MultiAgentApp.Agents;
using MultiAgentApp.Configuration;

namespace MultiAgentApp.Tests.Agents;

public class ProductsAgentTests
{
    private static Kernel CreateTestKernel()
    {
        var options = new AIOptions
        {
            UseFoundryLocal = true,
            FoundryLocal = new FoundryLocalOptions
            {
                Endpoint = "http://localhost:5272/v1",
                ModelId = "phi-4-mini-reasoning",
                ApiKey = "foundry-local"
            }
        };
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        return KernelFactory.CreateKernel(options, loggerFactory);
    }

    [Fact]
    public void Build_ReturnsAgentWithCorrectName()
    {
        var kernel = CreateTestKernel();
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var agent = new ProductsAgent(kernel, loggerFactory.CreateLogger<ProductsAgent>());

        var builtAgent = agent.Build();

        Assert.Equal("ProductsAgent", builtAgent.Name);
    }

    [Fact]
    public void Build_AgentHasInstructions()
    {
        var kernel = CreateTestKernel();
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var agent = new ProductsAgent(kernel, loggerFactory.CreateLogger<ProductsAgent>());

        var builtAgent = agent.Build();

        Assert.NotNull(builtAgent.Instructions);
        Assert.Contains("product", builtAgent.Instructions, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_CalledMultipleTimes_ReturnsDistinctAgents()
    {
        var kernel = CreateTestKernel();
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var agent = new ProductsAgent(kernel, loggerFactory.CreateLogger<ProductsAgent>());

        var agent1 = agent.Build();
        var agent2 = agent.Build();

        Assert.NotSame(agent1, agent2);
        Assert.Equal(agent1.Name, agent2.Name);
    }
}
