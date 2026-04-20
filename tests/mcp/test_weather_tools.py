"""Tests for weather MCP tool logic (no MCP transport required)."""

import pytest

from weather_mcp_server.tools.weather_tools import get_current_weather, get_weather_forecast


@pytest.mark.parametrize("city", ["Seattle", "Denver", "New York City"])
def test_get_current_weather_known_city_returns_data(city: str):
    result = get_current_weather(city)

    assert result is not None
    assert city.split()[0].lower() in result.lower()
    assert "Temperature" in result
    assert "Humidity" in result


def test_get_current_weather_unknown_city_returns_not_available():
    result = get_current_weather("Atlantis")

    assert "not available" in result.lower()


@pytest.mark.parametrize("city,days", [("Seattle", 5), ("Denver", 3), ("New York City", 1)])
def test_get_weather_forecast_known_city_returns_requested_days(city: str, days: int):
    result = get_weather_forecast(city, days)

    assert result is not None
    assert f"{days}-day forecast" in result.lower()


def test_get_weather_forecast_unknown_city_returns_not_available():
    result = get_weather_forecast("Atlantis")

    assert "not available" in result.lower()


@pytest.mark.parametrize("days", [0, -1, 10])
def test_get_weather_forecast_out_of_range_days_clamps_to_valid_range(days: int):
    result = get_weather_forecast("Seattle", days)

    assert result is not None
    assert "exception" not in result.lower()


def test_get_current_weather_temperatures_contain_both_scales():
    result = get_current_weather("Seattle")

    assert "\u00b0C" in result
    assert "\u00b0F" in result


def test_get_current_weather_case_insensitive():
    result_lower = get_current_weather("seattle")
    result_upper = get_current_weather("SEATTLE")
    result_mixed = get_current_weather("Seattle")

    assert result_lower == result_upper == result_mixed
