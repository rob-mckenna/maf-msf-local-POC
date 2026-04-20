"""Multi-Agent App – LangGraph-style Workflow entry point.

Run with:
    python -m multi_agent_app.main

Or, after ``pip install -e .``:
    multi-agent-app
"""

from __future__ import annotations

import asyncio
import json
import logging
import os
import uuid
from pathlib import Path

from multi_agent_app.agents.chat_client_factory import create_chat_client
from multi_agent_app.agents.orchestrator_agent import OrchestratorAgent
from multi_agent_app.agents.products_agent import ProductsAgent
from multi_agent_app.agents.weather_agent import WeatherAgent
from multi_agent_app.config.ai_options import AIOptions
from multi_agent_app.config.mcp_options import McpOptions
from multi_agent_app.config.telemetry_options import TelemetryOptions
from multi_agent_app.mcp.mcp_client_factory import McpClientFactory
from multi_agent_app.telemetry.telemetry_configuration import (
    build_tracer_provider,
    configure_logging,
)

_SETTINGS_DIR = Path(__file__).parent

# ── Demo queries ──────────────────────────────────────────────────────────────
_DEMO_QUERIES = [
    "What is the current weather in Seattle and do you have any waterproof jackets in stock?",
    (
        "I'm planning a camping trip to Denver next weekend. "
        "What will the weather be like, and what camping gear do you have available?"
    ),
    "Show me the 5-day forecast for New York City.",
]


def _deep_merge(base: dict, override: dict) -> dict:
    """Recursively merge ``override`` into a copy of ``base``."""
    result = dict(base)
    for key, value in override.items():
        if key in result and isinstance(result[key], dict) and isinstance(value, dict):
            result[key] = _deep_merge(result[key], value)
        else:
            result[key] = value
    return result


def _load_settings() -> dict:
    """Load settings.json (+ optional environment-specific overlay)."""
    with open(_SETTINGS_DIR / "settings.json") as f:
        settings: dict = json.load(f)

    env = os.getenv("PYTHON_ENV", "Production")
    overlay_path = _SETTINGS_DIR / f"settings.{env.lower()}.json"
    if overlay_path.exists():
        with open(overlay_path) as f:
            settings = _deep_merge(settings, json.load(f))

    return settings


async def main() -> None:
    """Async entry point for the multi-agent app."""

    # ── Configuration ─────────────────────────────────────────────────────────
    settings = _load_settings()
    ai_options = AIOptions.from_dict(settings.get("AI", {}))
    mcp_options = McpOptions.from_dict(settings.get("MCP", {}))
    telemetry_options = TelemetryOptions.from_dict(settings.get("Telemetry", {}))

    # ── Telemetry ─────────────────────────────────────────────────────────────
    configure_logging(telemetry_options)
    source_name = f"MultiAgentApp-{uuid.uuid4().hex}"
    tracer_provider = build_tracer_provider(telemetry_options, source_name)
    log = logging.getLogger("multi_agent_app")

    # ── Startup banner ────────────────────────────────────────────────────────
    print("\033[96m╔══════════════════════════════════════════════════════════╗\033[0m")
    print("\033[96m║      Multi-Agent App  –  LangGraph-style Workflow        ║\033[0m")
    print("\033[96m╚══════════════════════════════════════════════════════════╝\033[0m")

    ai_backend = (
        f"Foundry Local  ({ai_options.foundry_local.model_id}"
        f"  @ {ai_options.foundry_local.endpoint})"
        if ai_options.use_foundry_local
        else (
            f"Microsoft Foundry  (deployment: "
            f"{ai_options.microsoft_foundry.deployment_name}, "
            f"project: {ai_options.microsoft_foundry.project_name})"
        )
    )
    mcp_backend = (
        "Azure API Management MCP"
        if mcp_options.use_azure_apim
        else "Local stdio MCP servers"
    )
    app_insights = (
        "enabled"
        if telemetry_options.application_insights_connection_string
        else "disabled (set Telemetry.ApplicationInsightsConnectionString to enable)"
    )

    log.info("AI backend          : %s", ai_backend)
    log.info("MCP backend         : %s", mcp_backend)
    log.info("Application Insights: %s", app_insights)

    # ── AI clients ────────────────────────────────────────────────────────────
    log.info("Creating AI chat clients...")
    orchestrator_config = create_chat_client(ai_options)
    weather_config = create_chat_client(ai_options)
    products_config = create_chat_client(ai_options)

    # ── MCP tools + agents ────────────────────────────────────────────────────
    async with McpClientFactory(mcp_options) as mcp_factory:
        log.info("Connecting to MCP servers and loading tools...")
        weather_tool_set = await mcp_factory.get_weather_tools()
        products_tool_set = await mcp_factory.get_products_tools()
        log.info(
            "Loaded %d weather tools and %d products tools.",
            len(weather_tool_set.definitions),
            len(products_tool_set.definitions),
        )

        weather_agent = WeatherAgent(
            client=weather_config.client,
            model_id=weather_config.model_id,
            tool_definitions=weather_tool_set.definitions,
            tool_executor=weather_tool_set.executor,
        )
        products_agent = ProductsAgent(
            client=products_config.client,
            model_id=products_config.model_id,
            tool_definitions=products_tool_set.definitions,
            tool_executor=products_tool_set.executor,
        )
        orchestrator = OrchestratorAgent(
            client=orchestrator_config.client,
            model_id=orchestrator_config.model_id,
            weather_agent=weather_agent,
            products_agent=products_agent,
        )

        # ── Demo queries ──────────────────────────────────────────────────────
        log.info("Running %d demo queries.", len(_DEMO_QUERIES))
        for index, query in enumerate(_DEMO_QUERIES, start=1):
            print()
            print(
                f"\033[93m── Query {index} of {len(_DEMO_QUERIES)} "
                f"──────────────────────────────────────────────────\033[0m"
            )
            print(f"User: {query}")
            print()

            try:
                response = await orchestrator.run(query)
                print("\033[92mAssistant:\033[0m")
                print(response)
            except Exception as exc:  # noqa: BLE001
                log.error("Query %d failed: %s", index, exc, exc_info=True)
                print(f"\033[91mError: {exc}\033[0m")

    print()
    print("\033[96m╔══════════════════════════════════════════════════════════╗\033[0m")
    print("\033[96m║                     Demo Complete                        ║\033[0m")
    print("\033[96m╚══════════════════════════════════════════════════════════╝\033[0m")

    # Shutdown the tracer provider cleanly
    tracer_provider.shutdown()


def run() -> None:
    """Synchronous entry point registered in pyproject.toml [project.scripts]."""
    asyncio.run(main())


if __name__ == "__main__":
    run()
