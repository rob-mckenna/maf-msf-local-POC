"""Tests for create_chat_client factory."""

import pytest

from multi_agent_app.agents.chat_client_factory import ChatClientConfig, create_chat_client
from multi_agent_app.config.ai_options import (
    AIOptions,
    FoundryLocalOptions,
    MicrosoftFoundryOptions,
)


def test_create_with_foundry_local_returns_config():
    options = AIOptions(
        use_foundry_local=True,
        foundry_local=FoundryLocalOptions(
            endpoint="http://localhost:5272/v1",
            model_id="phi-4-mini-reasoning",
            api_key="foundry-local",
        ),
    )
    config = create_chat_client(options)

    assert config is not None
    assert isinstance(config, ChatClientConfig)
    assert config.client is not None
    assert config.model_id == "phi-4-mini-reasoning"


def test_create_with_microsoft_foundry_empty_endpoint_raises():
    options = AIOptions(
        use_foundry_local=False,
        microsoft_foundry=MicrosoftFoundryOptions(
            endpoint="",
            deployment_name="gpt-4o",
            project_name="my-project",
            api_key="test-key",
        ),
    )
    with pytest.raises(ValueError, match="endpoint is not configured"):
        create_chat_client(options)


def test_create_with_microsoft_foundry_valid_config_returns_config():
    options = AIOptions(
        use_foundry_local=False,
        microsoft_foundry=MicrosoftFoundryOptions(
            endpoint="https://myresource.openai.azure.com/",
            deployment_name="gpt-4o",
            project_name="my-project",
            api_key="test-key",
        ),
    )
    config = create_chat_client(options)

    assert config is not None
    assert config.model_id == "gpt-4o"


def test_create_with_microsoft_foundry_empty_api_key_uses_default_credential():
    # DefaultAzureCredential authenticates lazily on the first request;
    # construction must succeed without network access.
    options = AIOptions(
        use_foundry_local=False,
        microsoft_foundry=MicrosoftFoundryOptions(
            endpoint="https://myresource.openai.azure.com/",
            deployment_name="gpt-4o",
            project_name="my-project",
            api_key="",
        ),
    )
    config = create_chat_client(options)

    assert config is not None
    assert config.client is not None


def test_create_returns_different_instances_each_call():
    options = AIOptions(use_foundry_local=True)
    config1 = create_chat_client(options)
    config2 = create_chat_client(options)

    assert config1.client is not config2.client


def test_create_foundry_local_default_options_works():
    config = create_chat_client(AIOptions())

    assert config is not None
    assert config.model_id == "phi-4-mini-reasoning"
