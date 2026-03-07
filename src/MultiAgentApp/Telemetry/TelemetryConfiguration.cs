using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MultiAgentApp.Configuration;

namespace MultiAgentApp.Telemetry;

/// <summary>
/// Configures logging and observability for the application.
///
/// Today, telemetry is written to the console.  Once an Application Insights resource
/// is available in Azure:
///   1. Set <c>Telemetry:ApplicationInsightsConnectionString</c> in appsettings.json
///      (or as an environment variable / Azure Key Vault secret).
///   2. Add <c>services.AddApplicationInsightsTelemetryWorkerService()</c> to the
///      service collection (requires the Microsoft.ApplicationInsights NuGet package
///      already referenced in the project).
///   3. Remove the comment markers around the AppInsights registration below.
/// </summary>
public static class TelemetryConfiguration
{
    /// <summary>
    /// Registers logging providers based on <paramref name="options"/>.
    /// </summary>
    public static ILoggingBuilder ConfigureTelemetry(
        this ILoggingBuilder builder,
        TelemetryOptions options)
    {
        builder.AddConsole();

        // ── Application Insights ────────────────────────────────────────────────
        // Uncomment the block below once Azure Application Insights is available.
        //
        // if (!string.IsNullOrWhiteSpace(options.ApplicationInsightsConnectionString))
        // {
        //     builder.AddApplicationInsights(
        //         configureTelemetryConfiguration: tc =>
        //             tc.ConnectionString = options.ApplicationInsightsConnectionString,
        //         configureApplicationInsightsLoggerOptions: o => { });
        // }
        // ── End Application Insights ────────────────────────────────────────────

        if (options.EnableDetailedTracing)
        {
            builder.SetMinimumLevel(LogLevel.Debug);
        }

        return builder;
    }

    /// <summary>
    /// Registers the Application Insights telemetry worker service.
    /// Call this once an Application Insights resource is available.
    /// </summary>
    public static IServiceCollection AddApplicationInsightsTelemetry(
        this IServiceCollection services,
        TelemetryOptions options)
    {
        // ── Application Insights ────────────────────────────────────────────────
        // Uncomment once Microsoft.ApplicationInsights.AspNetCore (or
        // Microsoft.ApplicationInsights.WorkerService) is configured:
        //
        // if (!string.IsNullOrWhiteSpace(options.ApplicationInsightsConnectionString))
        // {
        //     services.AddApplicationInsightsTelemetryWorkerService(o =>
        //     {
        //         o.ConnectionString = options.ApplicationInsightsConnectionString;
        //         o.EnableAdaptiveSampling = true;
        //     });
        // }
        // ── End Application Insights ────────────────────────────────────────────

        return services;
    }
}
