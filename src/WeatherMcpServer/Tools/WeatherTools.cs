using System.ComponentModel;
using ModelContextProtocol.Server;

namespace WeatherMcpServer.Tools;

/// <summary>
/// MCP tools that provide mocked weather data.
///
/// These tools simulate a real Weather API integration.
/// In production, replace the mock data with real HTTP calls to a weather service
/// (e.g. OpenWeatherMap, National Weather Service, AccuWeather).
///
/// When Azure API Management MCP is available, these tools will be hosted behind
/// an APIM policy and the client will switch from stdio to HTTP/SSE transport.
/// </summary>
[McpServerToolType]
public sealed class WeatherTools
{
    private static readonly Dictionary<string, WeatherData> MockCurrentWeather = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Seattle"] = new("Seattle, WA", 12, 54, "Cloudy with light rain", 75, 15, "SW", false),
        ["Denver"] = new("Denver, CO", 8, 46, "Partly cloudy", 40, 20, "NW", false),
        ["New York City"] = new("New York City, NY", 18, 64, "Clear", 55, 10, "NE", false),
        ["New York"] = new("New York City, NY", 18, 64, "Clear", 55, 10, "NE", false),
        ["Chicago"] = new("Chicago, IL", 5, 41, "Windy and overcast", 60, 35, "N", true),
        ["Los Angeles"] = new("Los Angeles, CA", 22, 72, "Sunny", 25, 8, "W", false),
        ["Miami"] = new("Miami, FL", 28, 82, "Thunderstorms", 85, 18, "SE", true),
    };

    private static readonly Dictionary<string, List<ForecastDay>> MockForecasts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Seattle"] = [
            new("Monday", 14, 9, "Rainy"),
            new("Tuesday", 12, 8, "Heavy rain"),
            new("Wednesday", 15, 10, "Cloudy"),
            new("Thursday", 17, 11, "Partly cloudy"),
            new("Friday", 18, 12, "Mostly sunny"),
        ],
        ["Denver"] = [
            new("Monday", 10, 2, "Sunny"),
            new("Tuesday", 12, 3, "Partly cloudy"),
            new("Wednesday", 6, -1, "Snow showers"),
            new("Thursday", 4, -3, "Heavy snow"),
            new("Friday", 8, 0, "Clearing"),
        ],
        ["New York City"] = [
            new("Monday", 20, 14, "Clear"),
            new("Tuesday", 22, 15, "Sunny"),
            new("Wednesday", 18, 13, "Partly cloudy"),
            new("Thursday", 16, 12, "Overcast"),
            new("Friday", 14, 10, "Light rain"),
        ],
        ["New York"] = [
            new("Monday", 20, 14, "Clear"),
            new("Tuesday", 22, 15, "Sunny"),
            new("Wednesday", 18, 13, "Partly cloudy"),
            new("Thursday", 16, 12, "Overcast"),
            new("Friday", 14, 10, "Light rain"),
        ],
    };

    /// <summary>
    /// Gets the current weather conditions for a given city.
    /// </summary>
    [McpServerTool(Name = "get_current_weather"), Description("Get the current weather conditions for a city.")]
    public string GetCurrentWeather(
        [Description("The city name to get weather for (e.g. 'Seattle', 'Denver', 'New York City')")]
        string city)
    {
        if (!MockCurrentWeather.TryGetValue(city, out var weather))
        {
            return $"Weather data for '{city}' is not available. " +
                   $"Supported cities: {string.Join(", ", MockCurrentWeather.Keys)}.";
        }

        return $"""
                Current weather for {weather.Location}:
                  Condition  : {weather.Condition}
                  Temperature: {weather.TempCelsius}°C / {weather.TempFahrenheit}°F
                  Humidity   : {weather.HumidityPercent}%
                  Wind       : {weather.WindSpeedKmh} km/h from the {weather.WindDirection}
                  Alerts     : {(weather.HasAlert ? "⚠️  Weather advisory in effect – check local authorities." : "None")}
                """;
    }

    /// <summary>
    /// Gets a multi-day weather forecast for a given city.
    /// </summary>
    [McpServerTool(Name = "get_weather_forecast"), Description("Get a multi-day weather forecast for a city.")]
    public string GetWeatherForecast(
        [Description("The city name to get the forecast for")]
        string city,
        [Description("Number of forecast days to return (1–5, default 5)")]
        int days = 5)
    {
        if (!MockForecasts.TryGetValue(city, out var forecast))
        {
            return $"Forecast data for '{city}' is not available. " +
                   $"Supported cities: {string.Join(", ", MockForecasts.Keys)}.";
        }

        days = Math.Clamp(days, 1, 5);
        var selected = forecast.Take(days).ToList();

        var lines = selected.Select(d =>
            $"  {d.DayName,-12} High: {d.HighCelsius}°C / {CelsiusToFahrenheit(d.HighCelsius)}°F   " +
            $"Low: {d.LowCelsius}°C / {CelsiusToFahrenheit(d.LowCelsius)}°F   {d.Condition}");

        return $"{days}-day forecast for {city}:\n" + string.Join("\n", lines);
    }

    private static int CelsiusToFahrenheit(int celsius) => celsius * 9 / 5 + 32;

    // ── Internal data models ─────────────────────────────────────────────────

    private sealed record WeatherData(
        string Location,
        int TempCelsius,
        int TempFahrenheit,
        string Condition,
        int HumidityPercent,
        int WindSpeedKmh,
        string WindDirection,
        bool HasAlert);

    private sealed record ForecastDay(
        string DayName,
        int HighCelsius,
        int LowCelsius,
        string Condition);
}
