using WeatherMcpServer.Tools;

namespace MultiAgentApp.Tests.MCP;

/// <summary>
/// Tests for the mocked Weather MCP tools.
/// These tests exercise the tool logic directly (no MCP transport required).
/// </summary>
public class WeatherToolsTests
{
    private readonly WeatherTools _tools = new();

    [Theory]
    [InlineData("Seattle")]
    [InlineData("Denver")]
    [InlineData("New York City")]
    public void GetCurrentWeather_KnownCity_ReturnsWeatherData(string city)
    {
        var result = _tools.GetCurrentWeather(city);

        Assert.NotNull(result);
        Assert.Contains(city, result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Temperature", result);
        Assert.Contains("Humidity", result);
    }

    [Fact]
    public void GetCurrentWeather_UnknownCity_ReturnsNotAvailableMessage()
    {
        var result = _tools.GetCurrentWeather("Atlantis");

        Assert.Contains("not available", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Seattle", 5)]
    [InlineData("Denver", 3)]
    [InlineData("New York City", 1)]
    public void GetWeatherForecast_KnownCity_ReturnsRequestedDays(string city, int days)
    {
        var result = _tools.GetWeatherForecast(city, days);

        Assert.NotNull(result);
        Assert.Contains($"{days}-day forecast", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetWeatherForecast_UnknownCity_ReturnsNotAvailableMessage()
    {
        var result = _tools.GetWeatherForecast("Atlantis");

        Assert.Contains("not available", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10)]
    public void GetWeatherForecast_OutOfRangeDays_ClampsToValidRange(int days)
    {
        var result = _tools.GetWeatherForecast("Seattle", days);

        // Should not throw and should return valid data
        Assert.NotNull(result);
        Assert.DoesNotContain("exception", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetCurrentWeather_TemperaturesContainBothScales()
    {
        var result = _tools.GetCurrentWeather("Seattle");

        // Should contain both °C and °F
        Assert.Contains("°C", result);
        Assert.Contains("°F", result);
    }

    [Fact]
    public void GetCurrentWeather_CaseInsensitive()
    {
        var resultLower = _tools.GetCurrentWeather("seattle");
        var resultUpper = _tools.GetCurrentWeather("SEATTLE");
        var resultMixed = _tools.GetCurrentWeather("Seattle");

        Assert.Equal(resultLower, resultUpper);
        Assert.Equal(resultLower, resultMixed);
    }
}
