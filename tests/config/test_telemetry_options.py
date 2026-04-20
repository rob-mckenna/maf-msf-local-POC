"""Tests for TelemetryOptions."""

from multi_agent_app.config.telemetry_options import TelemetryOptions


def test_default_no_app_insights_connection_string():
    options = TelemetryOptions()
    assert options.application_insights_connection_string == ""


def test_default_detailed_tracing_enabled():
    options = TelemetryOptions()
    assert options.enable_detailed_tracing is True


def test_can_set_app_insights_connection_string():
    conn = (
        "InstrumentationKey=00000000-0000-0000-0000-000000000000;"
        "IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/"
    )
    options = TelemetryOptions(application_insights_connection_string=conn)
    assert options.application_insights_connection_string == conn


def test_app_insights_not_configured_when_string_is_empty():
    options = TelemetryOptions(application_insights_connection_string="")
    assert not options.application_insights_connection_string.strip()


def test_from_dict_parses_correctly():
    d = {
        "ApplicationInsightsConnectionString": "InstrumentationKey=abc",
        "EnableDetailedTracing": False,
    }
    options = TelemetryOptions.from_dict(d)
    assert options.application_insights_connection_string == "InstrumentationKey=abc"
    assert options.enable_detailed_tracing is False


def test_from_dict_empty_uses_defaults():
    options = TelemetryOptions.from_dict({})
    assert options.application_insights_connection_string == ""
    assert options.enable_detailed_tracing is True
