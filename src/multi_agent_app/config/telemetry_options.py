"""Configuration dataclasses for telemetry / OpenTelemetry settings."""

from __future__ import annotations
from dataclasses import dataclass, field


@dataclass
class TelemetryOptions:
    """Telemetry options.

    Application Insights connection is optional today; set the connection string
    once the Azure environment is available.
    """

    SECTION_NAME: str = field(
        default="Telemetry", init=False, repr=False, compare=False
    )

    application_insights_connection_string: str = ""
    """When non-empty, telemetry is sent to Azure Monitor Application Insights."""

    enable_detailed_tracing: bool = True
    """When True, detailed tool-call and orchestration traces are emitted.
    Disable in production to reduce costs."""

    @classmethod
    def from_dict(cls, d: dict) -> TelemetryOptions:
        return cls(
            application_insights_connection_string=d.get(
                "ApplicationInsightsConnectionString", ""
            ),
            enable_detailed_tracing=d.get("EnableDetailedTracing", True),
        )
