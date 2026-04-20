# Multi-Agent App – LangGraph-Style Orchestration + Foundry Local POC

A **Python 3.12** proof-of-concept demonstrating a **multi-agent workflow**
built with a **state-graph (LangGraph-style) orchestration pattern**, the
**Python MCP SDK**, and **Microsoft Foundry Local**, with a clear upgrade path
to **Microsoft Foundry** and **Azure API Management MCP** once those cloud
resources are available.

---

## Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                        multi_agent_app                         │
│                                                                │
│  main.py                                                       │
│    └─ OrchestratorAgent  (router → specialists → synthesizer)  │
│         ├─ WeatherAgent  ──► weather_mcp_server  (stdio/APIM)  │
│         └─ ProductsAgent ──► products_mcp_server (stdio/APIM)  │
│                                                                │
│  Telemetry: OpenTelemetry → console + optional AppInsights     │
└────────────────────────────────────────────────────────────────┘
```

### Packages

| Package | Description |
|---|---|
| `src/multi_agent_app` | Main orchestration app |
| `src/weather_mcp_server` | MCP server exposing mocked Weather API tools via stdio |
| `src/products_mcp_server` | MCP server exposing mocked Products API tools via stdio |
| `tests/` | pytest unit tests |

### Key dependencies

| Package | Purpose |
|---|---|
| `openai>=1.54` | Async OpenAI-compatible client (`AsyncOpenAI`, `AsyncAzureOpenAI`) |
| `azure-identity>=1.19` | `DefaultAzureCredential` for passwordless Azure auth |
| `mcp[cli]>=1.6` | MCP client + server SDK (`FastMCP`, `ClientSession`, `stdio_client`) |
| `opentelemetry-sdk>=1.29` | Tracing to console (always enabled) |
| `azure-monitor-opentelemetry-exporter` | Sends traces to Application Insights when configured |

---

## Getting Started

### Prerequisites

- Python 3.12+
- [Microsoft Foundry Local](https://learn.microsoft.com/azure/ai-foundry/foundry-local)

### Install

```bash
pip install -e ".[dev]"
```

### Running with Foundry Local

1. **Install and start Foundry Local**:
   ```bash
   winget install Microsoft.FoundryLocal
   foundry model run phi-4-mini-reasoning
   ```

2. **Run the app** from the repo root:
   ```bash
   python -m multi_agent_app.main
   ```
   The MCP servers are launched automatically as child processes.

---

## Switching to Microsoft Foundry

Edit `src/multi_agent_app/settings.json`:

```json
"AI": {
  "UseFoundryLocal": false,
  "MicrosoftFoundry": {
    "Endpoint":       "https://YOUR-RESOURCE.openai.azure.com/",
    "DeploymentName": "gpt-4o",
    "ProjectName":    "YOUR-PROJECT",
    "ApiKey":         "YOUR-API-KEY"
  }
}
```

Leave `ApiKey` empty to use `DefaultAzureCredential` (Managed Identity / Azure CLI).

---

## Switching MCP Servers to Azure API Management

1. Set `MCP.UseAzureApim = true` in settings.
2. Populate `MCP.WeatherServer.ApimEndpoint` / `MCP.ProductsServer.ApimEndpoint`.
3. Implement `_build_apim_transport` in `mcp_client_factory.py`.

---

## Enabling Application Insights

```json
"Telemetry": {
  "ApplicationInsightsConnectionString": "InstrumentationKey=..."
}
```

Restart the app — no code changes needed.

---

## Running Tests

```bash
pytest
```

Tests cover:

- `create_chat_client` – client creation for both AI backends
- `WeatherAgent` / `ProductsAgent` – graph node metadata and construction
- `WeatherTools` / `ProductsTools` – MCP server tool logic (no network)
- Configuration dataclasses – defaults and `from_dict` parsing

---

## Solution Structure

```
maf-msf-local-POC/
├── pyproject.toml
├── src/
│   ├── multi_agent_app/
│   │   ├── agents/
│   │   │   ├── chat_client_factory.py   ← AsyncOpenAI / AsyncAzureOpenAI factory
│   │   │   ├── weather_agent.py         ← Weather specialist graph node
│   │   │   ├── products_agent.py        ← Products specialist graph node
│   │   │   └── orchestrator_agent.py    ← Router / specialist / synthesizer graph
│   │   ├── config/
│   │   │   ├── ai_options.py
│   │   │   ├── mcp_options.py
│   │   │   └── telemetry_options.py
│   │   ├── mcp/
│   │   │   └── mcp_client_factory.py    ← Connects to MCP servers, returns McpToolSet
│   │   ├── telemetry/
│   │   │   └── telemetry_configuration.py ← OpenTelemetry + AppInsights-ready
│   │   ├── main.py
│   │   ├── settings.json
│   │   └── settings.development.json
│   ├── weather_mcp_server/
│   │   ├── server.py                    ← FastMCP server entry point
│   │   └── tools/weather_tools.py       ← Mocked weather tool implementations
│   └── products_mcp_server/
│       ├── server.py                    ← FastMCP server entry point
│       └── tools/products_tools.py      ← Mocked products tool implementations
└── tests/
    ├── agents/
    ├── config/
    └── mcp/
```
