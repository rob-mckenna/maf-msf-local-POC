"""Tests for ProductsAgent graph node."""

from openai import AsyncOpenAI

from multi_agent_app.agents.products_agent import ProductsAgent


def _make_client() -> AsyncOpenAI:
    return AsyncOpenAI(base_url="http://localhost:5272/v1", api_key="test")


def test_name_is_correct():
    agent = ProductsAgent(client=_make_client(), model_id="test-model")
    assert agent.name == "ProductsAgent"


def test_description_mentions_product():
    agent = ProductsAgent(client=_make_client(), model_id="test-model")
    assert "product" in agent.description.lower()


def test_construction_with_no_tools_does_not_raise():
    agent = ProductsAgent(client=_make_client(), model_id="test-model")
    assert agent is not None


def test_construction_with_tool_definitions_does_not_raise():
    tool_def = {
        "type": "function",
        "function": {
            "name": "get_product",
            "description": "Gets a product",
            "parameters": {
                "type": "object",
                "properties": {"id": {"type": "integer"}},
                "required": ["id"],
            },
        },
    }
    agent = ProductsAgent(
        client=_make_client(),
        model_id="test-model",
        tool_definitions=[tool_def],
    )
    assert agent is not None


def test_node_name_class_attribute_is_correct():
    assert ProductsAgent.node_name == "ProductsAgent"


def test_node_description_class_attribute_mentions_product():
    assert "product" in ProductsAgent.node_description.lower()
