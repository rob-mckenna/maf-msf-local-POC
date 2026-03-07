using MultiAgentApp.Configuration;

namespace MultiAgentApp.Tests.Configuration;

public class TelemetryOptionsTests
{
    [Fact]
    public void DefaultOptions_NoAppInsightsConnectionString()
    {
        var options = new TelemetryOptions();
        Assert.Equal(string.Empty, options.ApplicationInsightsConnectionString);
    }

    [Fact]
    public void DefaultOptions_DetailedTracingEnabled()
    {
        var options = new TelemetryOptions();
        Assert.True(options.EnableDetailedTracing);
    }

    [Fact]
    public void CanSetAppInsightsConnectionString()
    {
        const string connectionString =
            "InstrumentationKey=00000000-0000-0000-0000-000000000000;" +
            "IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/";

        var options = new TelemetryOptions
        {
            ApplicationInsightsConnectionString = connectionString
        };

        Assert.Equal(connectionString, options.ApplicationInsightsConnectionString);
    }

    [Fact]
    public void AppInsightsNotConfigured_WhenConnectionStringIsEmpty()
    {
        var options = new TelemetryOptions { ApplicationInsightsConnectionString = string.Empty };
        Assert.True(string.IsNullOrWhiteSpace(options.ApplicationInsightsConnectionString));
    }

    [Fact]
    public void SectionName_IsCorrect()
    {
        Assert.Equal("Telemetry", TelemetryOptions.SectionName);
    }
}
