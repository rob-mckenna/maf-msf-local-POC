"""Weather specialist graph node."""

from __future__ import annotations

import json
import logging
from collections.abc import Callable, Awaitable
from typing import Any

from openai import AsyncOpenAI

logger = logging.getLogger(__name__)

_NODE_NAME = "WeatherAgent"
_NODE_DESCRIPTION = "Specialist node for weather conditions, forecasts, and climate queries."
_MAX_TOOL_ITERATIONS = 10

_INSTRUCTIONS = """\
You are a weather specialist agent. Your job is to retrieve current weather
conditions and forecasts for requested locations by calling the available
weather tools.

Always:
- Use the get_current_weather_tool to fetch real-time conditions.
- Use the get_weather_forecast_tool to fetch multi-day forecasts.
- Format temperatures in both Celsius and Fahrenheit.
- Mention notable weather alerts or hazards if present.
- Keep responses concise and factual.

Do not fabricate weather data; always use the provided tools.
If no location is specified, ask the user to clarify.
"""


class WeatherAgent:
    """Weather specialist graph node used by the orchestrator.

    Args:
        client: Async OpenAI-compatible chat client.
        model_id: Model or deployment name.
        tool_definitions: List of tool definitions in OpenAI function-call
            format (may be empty when no MCP server is connected).
        tool_executor: Async callable ``(tool_name, args_dict) -> str`` that
            invokes the named tool through the MCP session.  May be ``None``
            when no tools are available.
    """

    node_name: str = _NODE_NAME
    node_description: str = _NODE_DESCRIPTION

    def __init__(
        self,
        client: AsyncOpenAI,
        model_id: str,
        tool_definitions: list[dict[str, Any]] | None = None,
        tool_executor: Callable[[str, dict[str, Any]], Awaitable[str]] | None = None,
    ) -> None:
        self._client = client
        self._model_id = model_id
        self._tool_definitions = tool_definitions or []
        self._tool_executor = tool_executor

    @property
    def name(self) -> str:
        return self.node_name

    @property
    def description(self) -> str:
        return self.node_description

    async def run(self, user_message: str) -> str:
        """Run the weather specialist for the given user message.

        Handles the full tool-calling loop: calls the model, executes any
        requested tools via the MCP session, and loops until the model
        produces a final text response.

        Args:
            user_message: The user's query about weather.

        Returns:
            The model's final text response, or a fallback message if the
            model produces no output.
        """
        messages: list[dict[str, Any]] = [
            {"role": "system", "content": _INSTRUCTIONS},
            {"role": "user", "content": user_message},
        ]
        tools = self._tool_definitions or None

        for _ in range(_MAX_TOOL_ITERATIONS):
            response = await self._client.chat.completions.create(
                model=self._model_id,
                messages=messages,  # type: ignore[arg-type]
                tools=tools,  # type: ignore[arg-type]
            )
            choice = response.choices[0]
            message = choice.message
            finish_reason = choice.finish_reason

            if finish_reason != "tool_calls" or not message.tool_calls:
                text = (message.content or "").strip()
                if not text:
                    logger.warning("%s returned an empty response.", self.name)
                    return "I couldn't retrieve weather details from the available tools."
                return text

            # Append the assistant message (preserving tool_calls)
            asst: dict[str, Any] = {"role": "assistant", "content": message.content}
            if message.tool_calls:
                asst["tool_calls"] = [
                    {
                        "id": tc.id,
                        "type": "function",
                        "function": {
                            "name": tc.function.name,
                            "arguments": tc.function.arguments,
                        },
                    }
                    for tc in message.tool_calls
                ]
            messages.append(asst)

            # Execute each tool call and append results
            for tc in message.tool_calls:
                args = json.loads(tc.function.arguments)
                if self._tool_executor is not None:
                    result = await self._tool_executor(tc.function.name, args)
                else:
                    result = f"Tool '{tc.function.name}' is not available."
                messages.append(
                    {
                        "role": "tool",
                        "tool_call_id": tc.id,
                        "content": result,
                    }
                )

        logger.warning("%s reached maximum tool iterations.", self.name)
        return "I couldn't complete the weather query within the allowed steps."
