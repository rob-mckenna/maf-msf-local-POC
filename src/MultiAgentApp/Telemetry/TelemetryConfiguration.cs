using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;
using MultiAgentApp.Configuration;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace MultiAgentApp.Telemetry;

/// <summary>
/// Configures OpenTelemetry tracing for the application.
///
/// Today, traces are written to the console.  Once an Application Insights resource
/// is available in Azure:
///   1. Set <c>Telemetry:ApplicationInsightsConnectionString</c> in configuration
///      (or as an environment variable / Azure Key Vault secret).
///   2. The Azure Monitor trace exporter is already wired up below — it activates
///      automatically when the connection string is non-empty.
///   No code changes are needed.
/// </summary>
public static class TelemetryConfiguration
{
    /// <summary>
    /// Builds and returns an <see cref="TracerProvider"/> configured for the app.
    /// The provider writes traces to the console and, when a connection string is
    /// configured, also to Azure Monitor Application Insights.
    /// </summary>
    /// <param name="options">Telemetry options from configuration.</param>
    /// <param name="sourceName">Activity source name used to correlate traces across graph nodes.</param>
    public static TracerProvider BuildTracerProvider(TelemetryOptions options, string sourceName)
    {
        var builder = Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddConsoleExporter();

        // ── Azure Monitor / Application Insights ──────────────────────────────
        // Activates automatically when the connection string is set in config.
        // No code change required — just set Telemetry:ApplicationInsightsConnectionString.
        if (!string.IsNullOrWhiteSpace(options.ApplicationInsightsConnectionString))
        {
            builder.AddAzureMonitorTraceExporter(o =>
                o.ConnectionString = options.ApplicationInsightsConnectionString);
        }
        // ── End Azure Monitor ─────────────────────────────────────────────────

        return builder.Build()!;
    }

    /// <summary>
    /// Configures the <see cref="ILoggingBuilder"/> log level based on
    /// <paramref name="options"/>.
    /// </summary>
    public static ILoggingBuilder ConfigureLogLevel(
        this ILoggingBuilder builder,
        TelemetryOptions options)
    {
        builder.AddConsole();
        builder.SetMinimumLevel(
            options.EnableDetailedTracing ? LogLevel.Debug : LogLevel.Information);
        return builder;
    }
}
