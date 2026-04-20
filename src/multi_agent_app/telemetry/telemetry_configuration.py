"""OpenTelemetry tracing configuration.

Traces are written to the console by default.  Once an Application Insights
resource is available in Azure:

1. Set ``Telemetry.ApplicationInsightsConnectionString`` in settings (or as an
   environment variable / Azure Key Vault secret).
2. Restart the app — no code changes needed.

The Azure Monitor trace exporter is already wired up below; it activates
automatically when the connection string is non-empty.
"""

from __future__ import annotations

import logging

from opentelemetry import trace
from opentelemetry.sdk.resources import Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import (
    BatchSpanProcessor,
    ConsoleSpanExporter,
    SimpleSpanProcessor,
)

from multi_agent_app.config.telemetry_options import TelemetryOptions


def build_tracer_provider(
    options: TelemetryOptions,
    source_name: str,
) -> TracerProvider:
    """Build and register a :class:`TracerProvider` for the application.

    The provider writes traces to the console and, when a connection string is
    configured, also to Azure Monitor Application Insights.

    Args:
        options: Telemetry options from configuration.
        source_name: Activity source name used to correlate traces across graph
            nodes.

    Returns:
        A configured :class:`TracerProvider` (already set as the global
        provider).
    """
    resource = Resource.create({"service.name": source_name})
    provider = TracerProvider(resource=resource)

    # Console exporter — always active.
    provider.add_span_processor(SimpleSpanProcessor(ConsoleSpanExporter()))

    # ── Azure Monitor / Application Insights ──────────────────────────────────
    # Activates automatically when the connection string is set in config.
    if options.application_insights_connection_string:
        try:
            from azure.monitor.opentelemetry.exporter import AzureMonitorTraceExporter

            exporter = AzureMonitorTraceExporter(
                connection_string=options.application_insights_connection_string
            )
            provider.add_span_processor(BatchSpanProcessor(exporter))
        except ImportError:
            logging.getLogger(__name__).warning(
                "azure-monitor-opentelemetry-exporter is not installed; "
                "Application Insights telemetry will not be sent."
            )
    # ── End Azure Monitor ─────────────────────────────────────────────────────

    trace.set_tracer_provider(provider)
    return provider


def configure_logging(options: TelemetryOptions) -> None:
    """Configure the root logger level based on telemetry options.

    Args:
        options: Telemetry options from configuration.
    """
    level = logging.DEBUG if options.enable_detailed_tracing else logging.INFO
    logging.basicConfig(
        level=level,
        format="%(asctime)s  %(levelname)-8s  %(name)s  %(message)s",
    )
