using Microsoft.Extensions.AI;
using MultiAgentApp.Agents;
using MultiAgentApp.Configuration;

namespace MultiAgentApp.Tests.Agents;

/// <summary>
/// Tests for <see cref="ChatClientFactory"/> – the factory that creates
/// <see cref="IChatClient"/> instances backed by Foundry Local or Microsoft Foundry.
/// </summary>
public class ChatClientFactoryTests
{
    [Fact]
    public void CreateChatClient_WithFoundryLocal_ReturnsIChatClient()
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

        // Should not throw – client is created with Foundry Local config (no network call yet)
        var client = ChatClientFactory.CreateChatClient(options);

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateChatClient_WithMicrosoftFoundry_EmptyEndpoint_Throws()
    {
        var options = new AIOptions
        {
            UseFoundryLocal = false,
            MicrosoftFoundry = new MicrosoftFoundryOptions
            {
                Endpoint = string.Empty,
                DeploymentName = "gpt-4o",
                ProjectName = "my-project",
                ApiKey = "test-key"
            }
        };

        Assert.Throws<InvalidOperationException>(() =>
            ChatClientFactory.CreateChatClient(options));
    }

    [Fact]
    public void CreateChatClient_WithMicrosoftFoundry_ValidConfig_ReturnsIChatClient()
    {
        var options = new AIOptions
        {
            UseFoundryLocal = false,
            MicrosoftFoundry = new MicrosoftFoundryOptions
            {
                Endpoint = "https://myresource.openai.azure.com/",
                DeploymentName = "gpt-4o",
                ProjectName = "my-project",
                ApiKey = "test-key"
            }
        };

        var client = ChatClientFactory.CreateChatClient(options);

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateChatClient_WithMicrosoftFoundry_EmptyApiKey_UsesDefaultCredentials_ReturnsIChatClient()
    {
        // When ApiKey is empty, DefaultAzureCredential should be used.
        // The client is created successfully; authentication only occurs on the first request.
        var options = new AIOptions
        {
            UseFoundryLocal = false,
            MicrosoftFoundry = new MicrosoftFoundryOptions
            {
                Endpoint = "https://myresource.openai.azure.com/",
                DeploymentName = "gpt-4o",
                ProjectName = "my-project",
                ApiKey = string.Empty
            }
        };

        var client = ChatClientFactory.CreateChatClient(options);

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateChatClient_ReturnsDifferentInstances_EachCall()
    {
        var options = new AIOptions { UseFoundryLocal = true };

        var client1 = ChatClientFactory.CreateChatClient(options);
        var client2 = ChatClientFactory.CreateChatClient(options);

        Assert.NotSame(client1, client2);
    }

    [Fact]
    public void CreateChatClient_FoundryLocal_DefaultOptions_Works()
    {
        var options = new AIOptions();

        // Default options use Foundry Local – should not throw
        var client = ChatClientFactory.CreateChatClient(options);

        Assert.NotNull(client);
    }
}
