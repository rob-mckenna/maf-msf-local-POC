"""Tests for McpOptions and McpServerOptions."""

from multi_agent_app.config.mcp_options import McpOptions, McpServerOptions


def test_default_options_use_stdio_transport():
    options = McpOptions()
    assert options.use_azure_apim is False


def test_default_weather_server_has_expected_name():
    options = McpOptions()
    assert options.weather_server.name == "WeatherMcpServer"


def test_default_products_server_has_expected_name():
    options = McpOptions()
    assert options.products_server.name == "ProductsMcpServer"


def test_default_weather_server_uses_python_command():
    options = McpOptions()
    assert options.weather_server.stdio_command == "python"
    assert options.weather_server.stdio_arguments  # non-empty


def test_default_products_server_uses_python_command():
    options = McpOptions()
    assert options.products_server.stdio_command == "python"
    assert options.products_server.stdio_arguments  # non-empty


def test_can_configure_apim_endpoint():
    options = McpOptions(
        use_azure_apim=True,
        weather_server=McpServerOptions(
            name="WeatherMcpServer",
            apim_endpoint="https://my-apim.azure-api.net/weather-mcp",
            apim_api_key="test-subscription-key",
        ),
    )
    assert options.use_azure_apim is True
    assert (
        options.weather_server.apim_endpoint
        == "https://my-apim.azure-api.net/weather-mcp"
    )


def test_from_dict_parses_correctly():
    d = {
        "UseAzureApim": False,
        "WeatherServer": {
            "Name": "WeatherMcpServer",
            "StdioCommand": "python",
            "StdioArguments": ["-m", "weather_mcp_server.server"],
        },
        "ProductsServer": {
            "Name": "ProductsMcpServer",
            "StdioCommand": "python",
            "StdioArguments": ["-m", "products_mcp_server.server"],
        },
    }
    options = McpOptions.from_dict(d)
    assert options.use_azure_apim is False
    assert options.weather_server.name == "WeatherMcpServer"
    assert options.products_server.name == "ProductsMcpServer"


def test_from_dict_empty_uses_defaults():
    options = McpOptions.from_dict({})
    assert options.use_azure_apim is False
    assert options.weather_server.name == "WeatherMcpServer"
    assert options.products_server.name == "ProductsMcpServer"
