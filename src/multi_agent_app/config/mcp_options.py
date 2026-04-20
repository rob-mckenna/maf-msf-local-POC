"""Configuration dataclasses for MCP (Model Context Protocol) server connections."""

from __future__ import annotations
from dataclasses import dataclass, field


@dataclass
class McpServerOptions:
    """Per-server MCP connection settings."""

    name: str = ""
    stdio_command: str = ""
    stdio_arguments: list[str] = field(default_factory=list)
    apim_endpoint: str = ""
    apim_api_key: str = ""

    @classmethod
    def from_dict(cls, d: dict) -> McpServerOptions:
        return cls(
            name=d.get("Name", ""),
            stdio_command=d.get("StdioCommand", ""),
            stdio_arguments=d.get("StdioArguments", []),
            apim_endpoint=d.get("ApimEndpoint", ""),
            apim_api_key=d.get("ApimApiKey", ""),
        )


@dataclass
class McpOptions:
    """Options for MCP server connections.

    Local stdio-based servers are used during development.  When Azure API
    Management MCP support becomes available, set ``use_azure_apim = True``
    and populate the APIM options.
    """

    SECTION_NAME: str = field(default="MCP", init=False, repr=False, compare=False)

    use_azure_apim: bool = False
    weather_server: McpServerOptions = field(
        default_factory=lambda: McpServerOptions(
            name="WeatherMcpServer",
            stdio_command="python",
            stdio_arguments=["-m", "weather_mcp_server.server"],
        )
    )
    products_server: McpServerOptions = field(
        default_factory=lambda: McpServerOptions(
            name="ProductsMcpServer",
            stdio_command="python",
            stdio_arguments=["-m", "products_mcp_server.server"],
        )
    )

    @classmethod
    def from_dict(cls, d: dict) -> McpOptions:
        return cls(
            use_azure_apim=d.get("UseAzureApim", False),
            weather_server=McpServerOptions.from_dict(d.get("WeatherServer", {}))
            if d.get("WeatherServer")
            else McpOptions().weather_server,
            products_server=McpServerOptions.from_dict(d.get("ProductsServer", {}))
            if d.get("ProductsServer")
            else McpOptions().products_server,
        )
