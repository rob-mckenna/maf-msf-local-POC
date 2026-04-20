"""Weather MCP server entry point.

Exposes mocked weather data via the Model Context Protocol (MCP).

Transport: stdio (default for local development).
When Azure API Management MCP support is available, this server can be
deployed as an HTTP/SSE endpoint and registered behind an APIM policy — no
code changes needed; the client switches transport via configuration.

Run with:
    python -m weather_mcp_server.server
"""

from mcp.server.fastmcp import FastMCP

from weather_mcp_server.tools.weather_tools import (
    get_current_weather,
    get_weather_forecast,
)

mcp = FastMCP("WeatherMcpServer")


@mcp.tool()
def get_current_weather_tool(
    city: str,
) -> str:
    """Get the current weather conditions for a city.

    Args:
        city: The city name to get weather for (e.g. 'Seattle', 'Denver',
              'New York City').
    """
    return get_current_weather(city)


@mcp.tool()
def get_weather_forecast_tool(
    city: str,
    days: int = 5,
) -> str:
    """Get a multi-day weather forecast for a city.

    Args:
        city: The city name to get the forecast for.
        days: Number of forecast days to return (1–5, default 5).
    """
    return get_weather_forecast(city, days)


def run() -> None:
    """Entry point registered in pyproject.toml [project.scripts]."""
    mcp.run()


if __name__ == "__main__":
    run()
