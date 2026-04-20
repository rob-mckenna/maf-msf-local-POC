"""MCP (Model Context Protocol) client factory.

Connects to MCP servers via stdio transport and exposes their tools as
OpenAI-compatible function definitions, together with an async executor that
calls the live MCP session.

Transport strategy
------------------
• Local (default): launches server executables as child processes via stdio.
• Azure APIM: switches to an HTTP/SSE transport once Azure API Management MCP
  endpoints become available.  Toggle ``MCP.UseAzureApim`` in settings.
"""

from __future__ import annotations

import logging
import os
from collections.abc import Awaitable, Callable
from contextlib import AsyncExitStack
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

from multi_agent_app.config.mcp_options import McpOptions, McpServerOptions

logger = logging.getLogger(__name__)

# Add the ``src/`` directory to PYTHONPATH of spawned MCP-server subprocesses
# so they can import their own packages as top-level modules.
_SRC_DIR = str(Path(__file__).parent.parent.parent)


@dataclass
class McpToolSet:
    """Tool definitions and their live MCP executor for one server.

    Attributes:
        definitions: List of tool schemas in OpenAI function-call format.
        executor: Async callable ``(tool_name, args_dict) -> result_str`` that
            calls the live MCP session.  ``None`` means no tools are available.
    """

    definitions: list[dict[str, Any]] = field(default_factory=list)
    executor: Callable[[str, dict[str, Any]], Awaitable[str]] | None = None


class McpClientFactory:
    """Creates and manages MCP client connections.

    Use as an async context manager to ensure all stdio sessions are cleanly
    torn down when the application exits::

        async with McpClientFactory(mcp_options) as factory:
            weather_tools = await factory.get_weather_tools()
            ...

    The ``McpToolSet.executor`` closures remain valid for the lifetime of the
    ``async with`` block.
    """

    def __init__(self, options: McpOptions) -> None:
        self._options = options
        self._exit_stack = AsyncExitStack()

    async def __aenter__(self) -> McpClientFactory:
        await self._exit_stack.__aenter__()
        return self

    async def __aexit__(self, *args: Any) -> None:
        await self._exit_stack.__aexit__(*args)

    async def get_weather_tools(self) -> McpToolSet:
        """Connect to the Weather MCP server and return its tools."""
        return await self._get_tools(self._options.weather_server)

    async def get_products_tools(self) -> McpToolSet:
        """Connect to the Products MCP server and return its tools."""
        return await self._get_tools(self._options.products_server)

    async def _get_tools(self, server_options: McpServerOptions) -> McpToolSet:
        if self._options.use_azure_apim:
            raise NotImplementedError(
                f"Azure APIM transport is not yet implemented for "
                f"'{server_options.name}'. "
                "Set MCP.UseAzureApim=false to continue using local stdio transport."
            )

        env = os.environ.copy()
        existing_pythonpath = env.get("PYTHONPATH", "")
        env["PYTHONPATH"] = (
            _SRC_DIR + os.pathsep + existing_pythonpath
            if existing_pythonpath
            else _SRC_DIR
        )

        server_params = StdioServerParameters(
            command=server_options.stdio_command,
            args=server_options.stdio_arguments,
            env=env,
        )

        logger.info(
            "Connecting to MCP server '%s' via stdio transport.",
            server_options.name,
        )

        read, write = await self._exit_stack.enter_async_context(
            stdio_client(server_params)
        )
        session: ClientSession = await self._exit_stack.enter_async_context(
            ClientSession(read, write)
        )
        await session.initialize()

        tools_result = await session.list_tools()
        tools = tools_result.tools

        definitions: list[dict[str, Any]] = [
            {
                "type": "function",
                "function": {
                    "name": t.name,
                    "description": t.description or "",
                    "parameters": t.inputSchema,
                },
            }
            for t in tools
        ]

        async def executor(name: str, args: dict[str, Any]) -> str:
            call_result = await session.call_tool(name, args)
            parts: list[str] = []
            for item in call_result.content:
                if hasattr(item, "text"):
                    parts.append(item.text)
                else:
                    parts.append(str(item))
            return "\n".join(parts)

        return McpToolSet(definitions=definitions, executor=executor)
