#pragma warning disable SKEXP0110 // Suppress experimental Semantic Kernel Agent Framework API warnings
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using MultiAgentApp.Agents;
using MultiAgentApp.Configuration;

namespace MultiAgentApp.Tests.Agents;

public class KernelFactoryTests
{
    [Fact]
    public void CreateKernel_WithFoundryLocal_ReturnsKernel()
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

        // Should not throw – kernel is created with Foundry Local config
        var kernel = KernelFactory.CreateKernel(options, loggerFactory);

        Assert.NotNull(kernel);
        Assert.NotNull(kernel.Services);
    }

    [Fact]
    public void CreateKernel_WithAzureAIFoundry_EmptyEndpoint_Throws()
    {
        var options = new AIOptions
        {
            UseFoundryLocal = false,
            AzureAIFoundry = new AzureAIFoundryOptions
            {
                Endpoint = string.Empty,
                DeploymentName = "gpt-4o",
                ApiKey = "test-key"
            }
        };

        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());

        // Should throw because the Azure endpoint is empty
        Assert.Throws<InvalidOperationException>(() =>
            KernelFactory.CreateKernel(options, loggerFactory));
    }

    [Fact]
    public void CreateKernel_WithAzureAIFoundry_ValidConfig_ReturnsKernel()
    {
        var options = new AIOptions
        {
            UseFoundryLocal = false,
            AzureAIFoundry = new AzureAIFoundryOptions
            {
                Endpoint = "https://myresource.openai.azure.com/",
                DeploymentName = "gpt-4o",
                ApiKey = "test-key"
            }
        };

        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());

        var kernel = KernelFactory.CreateKernel(options, loggerFactory);

        Assert.NotNull(kernel);
    }

    [Fact]
    public void CreateKernel_KernelCanBeCloned()
    {
        var options = new AIOptions { UseFoundryLocal = true };

        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var kernel = KernelFactory.CreateKernel(options, loggerFactory);

        // Cloning should not throw and should return a new Kernel
        var clone = kernel.Clone();
        Assert.NotNull(clone);
        Assert.NotSame(kernel, clone);
    }
}
