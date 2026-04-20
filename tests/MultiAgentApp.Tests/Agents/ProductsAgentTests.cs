using Microsoft.Extensions.AI;
using MultiAgentApp.Agents;
using MultiAgentApp.Configuration;

namespace MultiAgentApp.Tests.Agents;

public class ProductsAgentTests
{
    private static IChatClient CreateTestChatClient() =>
        ChatClientFactory.CreateChatClient(new AIOptions { UseFoundryLocal = true });

    [Fact]
    public void Metadata_HasExpectedName()
    {
        var client = CreateTestChatClient();
        var agent = new ProductsAgent(client, tools: []);

        Assert.Equal("ProductsAgent", agent.Name);
    }

    [Fact]
    public void Metadata_HasDescription()
    {
        var client = CreateTestChatClient();
        var agent = new ProductsAgent(client, tools: []);

        Assert.Contains("product", agent.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithNoTools_DoesNotThrow()
    {
        var client = CreateTestChatClient();
        var ex = Record.Exception(() => new ProductsAgent(client, tools: []));

        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_WithTools_DoesNotThrow()
    {
        var client = CreateTestChatClient();
        AITool mockTool = AIFunctionFactory.Create(
            ([System.ComponentModel.Description("Product ID")] int id) => $"Product {id}",
            "get_product",
            "Gets a product");

        var ex = Record.Exception(() => new ProductsAgent(client, tools: [mockTool]));
        Assert.Null(ex);
    }
}
