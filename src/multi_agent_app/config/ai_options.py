"""Configuration dataclasses for AI provider settings."""

from __future__ import annotations
from dataclasses import dataclass, field


@dataclass
class FoundryLocalOptions:
    """Connection settings for Microsoft Foundry Local (local inference).

    Foundry Local exposes an OpenAI-compatible REST API, typically at
    http://localhost:5272/v1.  No authentication is required; ``api_key`` is a
    placeholder satisfying the OpenAI client library validation.
    """

    endpoint: str = "http://localhost:5272/v1"
    model_id: str = "phi-4-mini-reasoning"
    api_key: str = "foundry-local"

    @classmethod
    def from_dict(cls, d: dict) -> FoundryLocalOptions:
        return cls(
            endpoint=d.get("Endpoint", "http://localhost:5272/v1"),
            model_id=d.get("ModelId", "phi-4-mini-reasoning"),
            api_key=d.get("ApiKey", "foundry-local"),
        )


@dataclass
class MicrosoftFoundryOptions:
    """Connection settings for Microsoft Foundry (cloud).

    Populate from environment variables or Azure Key Vault once a cloud
    environment is available.  Leave ``api_key`` empty to use
    ``DefaultAzureCredential`` (Managed Identity / Azure CLI / env vars).
    """

    endpoint: str = ""
    deployment_name: str = ""
    project_name: str = ""
    api_key: str = ""

    @classmethod
    def from_dict(cls, d: dict) -> MicrosoftFoundryOptions:
        return cls(
            endpoint=d.get("Endpoint", ""),
            deployment_name=d.get("DeploymentName", ""),
            project_name=d.get("ProjectName", ""),
            api_key=d.get("ApiKey", ""),
        )


@dataclass
class AIOptions:
    """Top-level AI provider options.

    Toggle ``use_foundry_local`` to switch between Microsoft Foundry Local
    (running on the developer's machine) and Microsoft Foundry (cloud).
    """

    SECTION_NAME: str = field(default="AI", init=False, repr=False, compare=False)

    use_foundry_local: bool = True
    foundry_local: FoundryLocalOptions = field(default_factory=FoundryLocalOptions)
    microsoft_foundry: MicrosoftFoundryOptions = field(
        default_factory=MicrosoftFoundryOptions
    )

    @classmethod
    def from_dict(cls, d: dict) -> AIOptions:
        return cls(
            use_foundry_local=d.get("UseFoundryLocal", True),
            foundry_local=FoundryLocalOptions.from_dict(d.get("FoundryLocal", {})),
            microsoft_foundry=MicrosoftFoundryOptions.from_dict(
                d.get("MicrosoftFoundry", {})
            ),
        )
