using Microsoft.Extensions.AI;
using MultiAgentApp.Agents;
using MultiAgentApp.Configuration;

namespace MultiAgentApp.Tests.Agents;

public class WeatherAgentTests
{
    private static IChatClient CreateTestChatClient() =>
        ChatClientFactory.CreateChatClient(new AIOptions { UseFoundryLocal = true });

    [Fact]
    public void Metadata_HasExpectedName()
    {
        var client = CreateTestChatClient();
        var agent = new WeatherAgent(client, tools: []);

        Assert.Equal("WeatherAgent", agent.Name);
    }

    [Fact]
    public void Metadata_HasDescription()
    {
        var client = CreateTestChatClient();
        var agent = new WeatherAgent(client, tools: []);

        Assert.Contains("weather", agent.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithNoTools_DoesNotThrow()
    {
        var client = CreateTestChatClient();
        var ex = Record.Exception(() => new WeatherAgent(client, tools: []));

        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_WithTools_DoesNotThrow()
    {
        var client = CreateTestChatClient();
        AITool mockTool = AIFunctionFactory.Create(
            ([System.ComponentModel.Description("Test param")] string input) => $"Echo: {input}",
            "test_tool",
            "A test tool");

        var ex = Record.Exception(() => new WeatherAgent(client, tools: [mockTool]));

        Assert.Null(ex);
    }
}
