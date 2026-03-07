using MultiAgentApp.Configuration;

namespace MultiAgentApp.Tests.Configuration;

public class McpOptionsTests
{
    [Fact]
    public void DefaultOptions_UseStdioTransport()
    {
        var options = new McpOptions();
        Assert.False(options.UseAzureApim);
    }

    [Fact]
    public void DefaultWeatherServer_HasExpectedName()
    {
        var options = new McpOptions();
        Assert.Equal("WeatherMcpServer", options.WeatherServer.Name);
    }

    [Fact]
    public void DefaultProductsServer_HasExpectedName()
    {
        var options = new McpOptions();
        Assert.Equal("ProductsMcpServer", options.ProductsServer.Name);
    }

    [Fact]
    public void DefaultWeatherServer_UsesStdioCommand()
    {
        var options = new McpOptions();
        Assert.Equal("dotnet", options.WeatherServer.StdioCommand);
        Assert.NotEmpty(options.WeatherServer.StdioArguments);
    }

    [Fact]
    public void DefaultProductsServer_UsesStdioCommand()
    {
        var options = new McpOptions();
        Assert.Equal("dotnet", options.ProductsServer.StdioCommand);
        Assert.NotEmpty(options.ProductsServer.StdioArguments);
    }

    [Fact]
    public void CanConfigureApimEndpoint()
    {
        var options = new McpOptions
        {
            UseAzureApim = true,
            WeatherServer = new McpServerOptions
            {
                Name = "WeatherMcpServer",
                ApimEndpoint = "https://my-apim.azure-api.net/weather-mcp",
                ApimApiKey = "test-subscription-key"
            }
        };

        Assert.True(options.UseAzureApim);
        Assert.Equal("https://my-apim.azure-api.net/weather-mcp", options.WeatherServer.ApimEndpoint);
    }

    [Fact]
    public void SectionName_IsCorrect()
    {
        Assert.Equal("MCP", McpOptions.SectionName);
    }
}
