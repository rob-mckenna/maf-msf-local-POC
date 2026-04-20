"""Factory for OpenAI-compatible async chat clients.

Switching between Microsoft Foundry Local (local) and Microsoft Foundry (cloud)
is controlled solely by ``AIOptions.use_foundry_local`` in configuration.
No code changes are needed to flip between backends.
"""

from __future__ import annotations
from dataclasses import dataclass

from openai import AsyncAzureOpenAI, AsyncOpenAI

from multi_agent_app.config.ai_options import AIOptions


@dataclass
class ChatClientConfig:
    """Bundles an async OpenAI-compatible client with its model/deployment ID."""

    client: AsyncOpenAI
    """Async client for chat completion requests (``AsyncOpenAI`` or
    ``AsyncAzureOpenAI``)."""

    model_id: str
    """Model name or Azure deployment name to pass in every completion request."""


def create_chat_client(options: AIOptions) -> ChatClientConfig:
    """Create an async chat client from configuration.

    Args:
        options: AI provider options loaded from settings.

    Returns:
        A :class:`ChatClientConfig` ready for use by orchestration graph nodes.

    Raises:
        ValueError: When Microsoft Foundry is selected but no endpoint is
            configured.
    """
    if options.use_foundry_local:
        client = AsyncOpenAI(
            base_url=options.foundry_local.endpoint,
            api_key=options.foundry_local.api_key,
        )
        return ChatClientConfig(client=client, model_id=options.foundry_local.model_id)

    # ── Microsoft Foundry (cloud) ─────────────────────────────────────────────
    opts = options.microsoft_foundry
    if not opts.endpoint:
        raise ValueError(
            "Microsoft Foundry endpoint is not configured. "
            "Set AI.MicrosoftFoundry.Endpoint, DeploymentName, and ProjectName in "
            "settings, or set AI.UseFoundryLocal=true to use Foundry Local instead."
        )

    if not opts.api_key:
        # Use DefaultAzureCredential (Managed Identity, Azure CLI, env vars, …).
        # Preferred for Azure deployments; authentication happens lazily on the
        # first request, not during construction.
        from azure.identity import DefaultAzureCredential, get_bearer_token_provider

        credential = DefaultAzureCredential()
        token_provider = get_bearer_token_provider(
            credential, "https://cognitiveservices.azure.com/.default"
        )
        client = AsyncAzureOpenAI(
            azure_endpoint=opts.endpoint,
            azure_ad_token_provider=token_provider,
            api_version="2024-02-01",
        )
    else:
        client = AsyncAzureOpenAI(
            azure_endpoint=opts.endpoint,
            api_key=opts.api_key,
            api_version="2024-02-01",
        )

    return ChatClientConfig(client=client, model_id=opts.deployment_name)
