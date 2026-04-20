"""Tests for AIOptions, FoundryLocalOptions, and MicrosoftFoundryOptions."""

from multi_agent_app.config.ai_options import (
    AIOptions,
    FoundryLocalOptions,
    MicrosoftFoundryOptions,
)


def test_default_options_use_foundry_local():
    options = AIOptions()
    assert options.use_foundry_local is True


def test_default_foundry_local_options():
    opts = FoundryLocalOptions()
    assert opts.endpoint == "http://localhost:5272/v1"
    assert opts.model_id == "phi-4-mini-reasoning"
    assert opts.api_key == "foundry-local"


def test_default_microsoft_foundry_options_are_empty():
    opts = MicrosoftFoundryOptions()
    assert opts.endpoint == ""
    assert opts.deployment_name == ""
    assert opts.project_name == ""
    assert opts.api_key == ""


def test_ai_options_can_override_foundry_local_endpoint():
    options = AIOptions(
        use_foundry_local=True,
        foundry_local=FoundryLocalOptions(
            endpoint="http://custom-host:5272/v1",
            model_id="custom-model",
        ),
    )
    assert options.foundry_local.endpoint == "http://custom-host:5272/v1"
    assert options.foundry_local.model_id == "custom-model"


def test_ai_options_can_switch_to_microsoft_foundry():
    options = AIOptions(
        use_foundry_local=False,
        microsoft_foundry=MicrosoftFoundryOptions(
            endpoint="https://myresource.openai.azure.com/",
            deployment_name="gpt-4o",
            project_name="my-project",
            api_key="test-key",
        ),
    )
    assert options.use_foundry_local is False
    assert options.microsoft_foundry.endpoint == "https://myresource.openai.azure.com/"
    assert options.microsoft_foundry.deployment_name == "gpt-4o"
    assert options.microsoft_foundry.project_name == "my-project"


def test_from_dict_foundry_local():
    d = {
        "UseFoundryLocal": True,
        "FoundryLocal": {
            "Endpoint": "http://localhost:5272/v1",
            "ModelId": "phi-4-mini-reasoning",
            "ApiKey": "foundry-local",
        },
        "MicrosoftFoundry": {
            "Endpoint": "",
            "DeploymentName": "",
            "ProjectName": "",
            "ApiKey": "",
        },
    }
    options = AIOptions.from_dict(d)
    assert options.use_foundry_local is True
    assert options.foundry_local.endpoint == "http://localhost:5272/v1"
    assert options.foundry_local.model_id == "phi-4-mini-reasoning"


def test_from_dict_microsoft_foundry():
    d = {
        "UseFoundryLocal": False,
        "FoundryLocal": {},
        "MicrosoftFoundry": {
            "Endpoint": "https://example.openai.azure.com/",
            "DeploymentName": "gpt-4o",
            "ProjectName": "proj",
            "ApiKey": "key",
        },
    }
    options = AIOptions.from_dict(d)
    assert options.use_foundry_local is False
    assert options.microsoft_foundry.endpoint == "https://example.openai.azure.com/"
    assert options.microsoft_foundry.deployment_name == "gpt-4o"


def test_from_dict_empty_uses_defaults():
    options = AIOptions.from_dict({})
    assert options.use_foundry_local is True
    assert options.foundry_local.endpoint == "http://localhost:5272/v1"
