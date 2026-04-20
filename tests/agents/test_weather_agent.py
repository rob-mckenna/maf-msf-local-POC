"""Tests for WeatherAgent graph node."""

from openai import AsyncOpenAI

from multi_agent_app.agents.weather_agent import WeatherAgent


def _make_client() -> AsyncOpenAI:
    return AsyncOpenAI(base_url="http://localhost:5272/v1", api_key="test")


def test_name_is_correct():
    agent = WeatherAgent(client=_make_client(), model_id="test-model")
    assert agent.name == "WeatherAgent"


def test_description_mentions_weather():
    agent = WeatherAgent(client=_make_client(), model_id="test-model")
    assert "weather" in agent.description.lower()


def test_construction_with_no_tools_does_not_raise():
    agent = WeatherAgent(client=_make_client(), model_id="test-model")
    assert agent is not None


def test_construction_with_tool_definitions_does_not_raise():
    tool_def = {
        "type": "function",
        "function": {
            "name": "test_tool",
            "description": "A test tool",
            "parameters": {
                "type": "object",
                "properties": {"input": {"type": "string"}},
                "required": ["input"],
            },
        },
    }
    agent = WeatherAgent(
        client=_make_client(),
        model_id="test-model",
        tool_definitions=[tool_def],
    )
    assert agent is not None


def test_node_name_class_attribute_is_correct():
    assert WeatherAgent.node_name == "WeatherAgent"


def test_node_description_class_attribute_mentions_weather():
    assert "weather" in WeatherAgent.node_description.lower()
