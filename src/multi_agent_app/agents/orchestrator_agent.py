"""Orchestrator graph node – routes to specialist nodes and synthesises results."""

from __future__ import annotations

import logging
from dataclasses import dataclass, field
from typing import Any

from openai import AsyncOpenAI

from multi_agent_app.agents.products_agent import ProductsAgent
from multi_agent_app.agents.weather_agent import WeatherAgent

logger = logging.getLogger(__name__)

_SYNTHESIS_INSTRUCTIONS = """\
You are an orchestrator that combines specialist outputs into one final response.
Provide a concise, clear answer and avoid repeating the same points.
If either specialist reports uncertainty, preserve that uncertainty in the final
answer.
"""

_WEATHER_TERMS = frozenset(
    {"weather", "forecast", "temperature", "rain", "snow", "climate"}
)
_PRODUCT_TERMS = frozenset(
    {"product", "inventory", "stock", "price", "buy", "catalog", "gear", "jacket"}
)


@dataclass
class _GraphState:
    user_message: str
    needs_weather: bool = False
    needs_products: bool = False
    weather_response: str = ""
    products_response: str = ""


class OrchestratorAgent:
    """Orchestrates specialist nodes through a state-graph style flow.

    Graph flow
    ----------
    1. **Router node** – inspects the user message and sets ``needs_weather``
       and/or ``needs_products`` flags.
    2. **Specialist nodes** – ``WeatherAgent`` and/or ``ProductsAgent`` run as
       required.
    3. **Synthesis node** – the orchestrator's own chat client combines the
       specialist outputs into one final answer.

    Args:
        client: Async OpenAI-compatible chat client for the synthesis step.
        model_id: Model or deployment name.
        weather_agent: Weather specialist node.
        products_agent: Products specialist node.
    """

    def __init__(
        self,
        client: AsyncOpenAI,
        model_id: str,
        weather_agent: WeatherAgent,
        products_agent: ProductsAgent,
    ) -> None:
        self._client = client
        self._model_id = model_id
        self._weather_agent = weather_agent
        self._products_agent = products_agent

    async def run(self, user_message: str) -> str:
        """Run the orchestration graph for a user message.

        Args:
            user_message: The user's query (may span weather and/or products).

        Returns:
            A synthesised final response from the orchestrator.
        """
        logger.info("Starting orchestration graph for: %s", user_message)

        state = _GraphState(user_message=user_message)
        self._route(state)

        if state.needs_weather:
            state.weather_response = await self._weather_agent.run(user_message)

        if state.needs_products:
            state.products_response = await self._products_agent.run(user_message)

        result = await self._synthesize(state)
        logger.info("Orchestration graph complete.")
        return result

    def _route(self, state: _GraphState) -> None:
        text = state.user_message.lower()
        state.needs_weather = any(t in text for t in _WEATHER_TERMS)
        state.needs_products = any(t in text for t in _PRODUCT_TERMS)

        if not state.needs_weather and not state.needs_products:
            logger.info(
                "Router found no explicit specialist terms for input; "
                "routing to both specialists by default."
            )
            state.needs_weather = True
            state.needs_products = True

    async def _synthesize(self, state: _GraphState) -> str:
        if not state.needs_weather and not state.needs_products:
            return "I couldn't determine which specialists to route to."

        summaries: list[str] = []
        if state.weather_response:
            summaries.append(
                f"[{self._weather_agent.name}] {state.weather_response}"
            )
        if state.products_response:
            summaries.append(
                f"[{self._products_agent.name}] {state.products_response}"
            )

        if not summaries:
            return "The workflow completed but no specialist output was generated."

        combined = "\n".join(summaries)
        messages: list[dict[str, Any]] = [
            {"role": "system", "content": _SYNTHESIS_INSTRUCTIONS},
            {"role": "user", "content": f"Original user question: {state.user_message}"},
            {"role": "user", "content": f"Specialist outputs:\n{combined}"},
        ]

        response = await self._client.chat.completions.create(
            model=self._model_id,
            messages=messages,  # type: ignore[arg-type]
        )
        text = (response.choices[0].message.content or "").strip()
        return text if text else combined
