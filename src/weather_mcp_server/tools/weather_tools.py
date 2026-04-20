"""Mocked weather data tools used by the Weather MCP server.

These functions simulate a real Weather API integration.
In production, replace the mock data with real HTTP calls to a weather service
(e.g. OpenWeatherMap, National Weather Service, AccuWeather).

When Azure API Management MCP is available, the server hosting these tools can
be deployed as an HTTP/SSE endpoint and registered behind an APIM policy —
no code changes needed; the client switches transport via configuration.
"""

from __future__ import annotations
from dataclasses import dataclass


@dataclass(frozen=True)
class _WeatherData:
    location: str
    temp_celsius: int
    temp_fahrenheit: int
    condition: str
    humidity_percent: int
    wind_speed_kmh: int
    wind_direction: str
    has_alert: bool


@dataclass(frozen=True)
class _ForecastDay:
    day_name: str
    high_celsius: int
    low_celsius: int
    condition: str


_MOCK_CURRENT_WEATHER: dict[str, _WeatherData] = {
    k: _WeatherData(*v)
    for k, v in {
        "seattle": ("Seattle, WA", 12, 54, "Cloudy with light rain", 75, 15, "SW", False),
        "denver": ("Denver, CO", 8, 46, "Partly cloudy", 40, 20, "NW", False),
        "new york city": ("New York City, NY", 18, 64, "Clear", 55, 10, "NE", False),
        "new york": ("New York City, NY", 18, 64, "Clear", 55, 10, "NE", False),
        "chicago": ("Chicago, IL", 5, 41, "Windy and overcast", 60, 35, "N", True),
        "los angeles": ("Los Angeles, CA", 22, 72, "Sunny", 25, 8, "W", False),
        "miami": ("Miami, FL", 28, 82, "Thunderstorms", 85, 18, "SE", True),
    }.items()
}

_MOCK_FORECASTS: dict[str, list[_ForecastDay]] = {
    "seattle": [
        _ForecastDay("Monday", 14, 9, "Rainy"),
        _ForecastDay("Tuesday", 12, 8, "Heavy rain"),
        _ForecastDay("Wednesday", 15, 10, "Cloudy"),
        _ForecastDay("Thursday", 17, 11, "Partly cloudy"),
        _ForecastDay("Friday", 18, 12, "Mostly sunny"),
    ],
    "denver": [
        _ForecastDay("Monday", 10, 2, "Sunny"),
        _ForecastDay("Tuesday", 12, 3, "Partly cloudy"),
        _ForecastDay("Wednesday", 6, -1, "Snow showers"),
        _ForecastDay("Thursday", 4, -3, "Heavy snow"),
        _ForecastDay("Friday", 8, 0, "Clearing"),
    ],
    "new york city": [
        _ForecastDay("Monday", 20, 14, "Clear"),
        _ForecastDay("Tuesday", 22, 15, "Sunny"),
        _ForecastDay("Wednesday", 18, 13, "Partly cloudy"),
        _ForecastDay("Thursday", 16, 12, "Overcast"),
        _ForecastDay("Friday", 14, 10, "Light rain"),
    ],
    "new york": [
        _ForecastDay("Monday", 20, 14, "Clear"),
        _ForecastDay("Tuesday", 22, 15, "Sunny"),
        _ForecastDay("Wednesday", 18, 13, "Partly cloudy"),
        _ForecastDay("Thursday", 16, 12, "Overcast"),
        _ForecastDay("Friday", 14, 10, "Light rain"),
    ],
}


def _celsius_to_fahrenheit(celsius: int) -> int:
    return celsius * 9 // 5 + 32


def get_current_weather(city: str) -> str:
    """Get the current weather conditions for a city.

    Args:
        city: The city name (e.g. 'Seattle', 'Denver', 'New York City').

    Returns:
        A formatted string with current conditions, or an error message if
        the city is not available.
    """
    key = city.strip().lower()
    weather = _MOCK_CURRENT_WEATHER.get(key)
    if weather is None:
        supported = ", ".join(
            c.title() for c in sorted(set(_MOCK_CURRENT_WEATHER.keys()))
        )
        return (
            f"Weather data for '{city}' is not available. "
            f"Supported cities: {supported}."
        )

    alert = (
        "\u26a0\ufe0f  Weather advisory in effect \u2013 check local authorities."
        if weather.has_alert
        else "None"
    )
    return (
        f"Current weather for {weather.location}:\n"
        f"  Condition  : {weather.condition}\n"
        f"  Temperature: {weather.temp_celsius}\u00b0C / {weather.temp_fahrenheit}\u00b0F\n"
        f"  Humidity   : {weather.humidity_percent}%\n"
        f"  Wind       : {weather.wind_speed_kmh} km/h from the {weather.wind_direction}\n"
        f"  Alerts     : {alert}"
    )


def get_weather_forecast(city: str, days: int = 5) -> str:
    """Get a multi-day weather forecast for a city.

    Args:
        city: The city name to get the forecast for.
        days: Number of forecast days to return (1–5, default 5).

    Returns:
        A formatted forecast string, or an error message if the city is not
        available.
    """
    key = city.strip().lower()
    forecast = _MOCK_FORECASTS.get(key)
    if forecast is None:
        supported = ", ".join(
            c.title() for c in sorted(set(_MOCK_FORECASTS.keys()))
        )
        return (
            f"Forecast data for '{city}' is not available. "
            f"Supported cities: {supported}."
        )

    days = max(1, min(days, 5))
    selected = forecast[:days]
    lines = [
        f"  {d.day_name:<12} High: {d.high_celsius}\u00b0C / "
        f"{_celsius_to_fahrenheit(d.high_celsius)}\u00b0F   "
        f"Low: {d.low_celsius}\u00b0C / "
        f"{_celsius_to_fahrenheit(d.low_celsius)}\u00b0F   {d.condition}"
        for d in selected
    ]
    return f"{days}-day forecast for {city}:\n" + "\n".join(lines)
