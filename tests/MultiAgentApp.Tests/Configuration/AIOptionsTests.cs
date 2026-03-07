using MultiAgentApp.Configuration;

namespace MultiAgentApp.Tests.Configuration;

public class AIOptionsTests
{
    [Fact]
    public void DefaultOptions_UseFoundryLocal()
    {
        var options = new AIOptions();
        Assert.True(options.UseFoundryLocal);
    }

    [Fact]
    public void DefaultFoundryLocalOptions_HasExpectedDefaults()
    {
        var opts = new FoundryLocalOptions();
        Assert.Equal("http://localhost:5272/v1", opts.Endpoint);
        Assert.Equal("phi-4-mini-reasoning", opts.ModelId);
        Assert.Equal("foundry-local", opts.ApiKey);
    }

    [Fact]
    public void DefaultMicrosoftFoundryOptions_AreEmpty()
    {
        var opts = new MicrosoftFoundryOptions();
        Assert.Equal(string.Empty, opts.Endpoint);
        Assert.Equal(string.Empty, opts.DeploymentName);
        Assert.Equal(string.Empty, opts.ProjectName);
        Assert.Equal(string.Empty, opts.ApiKey);
    }

    [Fact]
    public void AIOptions_CanOverrideFoundryLocalEndpoint()
    {
        var options = new AIOptions
        {
            UseFoundryLocal = true,
            FoundryLocal = new FoundryLocalOptions
            {
                Endpoint = "http://custom-host:5272/v1",
                ModelId = "custom-model"
            }
        };

        Assert.Equal("http://custom-host:5272/v1", options.FoundryLocal.Endpoint);
        Assert.Equal("custom-model", options.FoundryLocal.ModelId);
    }

    [Fact]
    public void AIOptions_CanSwitchToMicrosoftFoundry()
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

        Assert.False(options.UseFoundryLocal);
        Assert.Equal("https://myresource.openai.azure.com/", options.MicrosoftFoundry.Endpoint);
        Assert.Equal("gpt-4o", options.MicrosoftFoundry.DeploymentName);
        Assert.Equal("my-project", options.MicrosoftFoundry.ProjectName);
    }

    [Fact]
    public void SectionName_IsCorrect()
    {
        Assert.Equal("AI", AIOptions.SectionName);
    }
}
