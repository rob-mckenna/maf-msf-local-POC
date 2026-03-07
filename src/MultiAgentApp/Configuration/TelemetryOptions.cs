namespace MultiAgentApp.Configuration;

/// <summary>
/// Telemetry options.
/// Application Insights connection is optional today; set the connection string once
/// the Azure environment is available.
/// </summary>
public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    /// <summary>
    /// Application Insights connection string.
    /// When non-empty, telemetry is sent to Azure Monitor Application Insights.
    /// Retrieve from the Azure portal or Key Vault; do not hard-code in source.
    /// </summary>
    public string ApplicationInsightsConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// When <c>true</c>, detailed Microsoft Agent Framework activity traces are emitted
    /// (tool calls, token counts, latencies). Disable in production to reduce costs.
    /// </summary>
    public bool EnableDetailedTracing { get; set; } = true;
}
