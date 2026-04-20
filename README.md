# Multi-Agent App – LangGraph-Style Orchestration + Foundry Local POC

A **C# .NET 8** proof-of-concept demonstrating a **multi-agent workflow** built with a
**state-graph (LangGraph-style) orchestration pattern**, plus **Microsoft Foundry Local**,
with a clear upgrade path to **Microsoft Foundry** and
**Azure API Management MCP** once those cloud resources are available.

---

## Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                        MultiAgentApp                           │
│                                                                │
│  Program.cs                                                    │
│    └─ OrchestratorAgent  (state-graph routing + synthesis)    │
│         ├─ WeatherAgent  ──► WeatherMcpServer (stdio / APIM)  │
│         └─ ProductsAgent ──► ProductsMcpServer (stdio / APIM) │
│                                                                │
│  Telemetry: OpenTelemetry → console + optional AppInsights     │
└────────────────────────────────────────────────────────────────┘
```

### Projects

| Project | Description |
|---|---|
| `src/MultiAgentApp` | Main orchestration app using state-graph orchestration |
| `src/WeatherMcpServer` | MCP server exposing mocked Weather API tools via stdio |
| `src/ProductsMcpServer` | MCP server exposing mocked Products API tools via stdio |
| `tests/MultiAgentApp.Tests` | xUnit unit tests |

### Key Packages

| Package | Purpose |
|---|---|
| `Microsoft.Extensions.AI.OpenAI` | `AsIChatClient()` for OpenAI-compatible endpoints |
| `ModelContextProtocol` (1.1.0) | MCP client (stdio now; APIM-ready) |
| `OpenTelemetry` + `Azure.Monitor.OpenTelemetry.Exporter` | Tracing (AppInsights-ready) |

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Microsoft Foundry Local](https://learn.microsoft.com/azure/ai-foundry/foundry-local) (for local mode)

### Running with Foundry Local

1. **Install and start Foundry Local**:
   ```bash
   # Install (one-time)
   winget install Microsoft.FoundryLocal

   # Pull and start a model
   foundry model run phi-4-mini-reasoning
   ```

2. **Build and run**:
   ```bash
   dotnet run --project src/MultiAgentApp
   ```
   The app auto-launches the Weather and Products MCP servers as child processes.

---

## Switching to Microsoft Foundry

When a Microsoft Foundry resource is available, flip a single config flag:

1. Edit `src/MultiAgentApp/appsettings.json`:
   ```json
   "AI": {
     "UseFoundryLocal": false,
     "MicrosoftFoundry": {
       "Endpoint":        "https://YOUR-RESOURCE.openai.azure.com/",
       "DeploymentName":  "gpt-4o",
       "ProjectName":     "YOUR-PROJECT-NAME",
       "ApiKey":          "YOUR-API-KEY"
     }
   }
   ```
   > For Managed Identity / passwordless auth, see the comment block in
   > `src/MultiAgentApp/Agents/ChatClientFactory.cs`.

2. No code changes required.

---

## Switching MCP Servers to Azure API Management

When Azure API Management MCP support is available:

1. Set `MCP:UseAzureApim = true` in configuration.
2. Populate `MCP:WeatherServer:ApimEndpoint` and `MCP:ProductsServer:ApimEndpoint`.
3. Uncomment the `BuildApimTransport` implementation in `McpClientFactory.cs`.

---

## Enabling Application Insights

Telemetry is already wired up via OpenTelemetry. To activate Application Insights:

1. Set the connection string in configuration (environment variable preferred in production):
   ```json
   "Telemetry": {
     "ApplicationInsightsConnectionString": "InstrumentationKey=..."
   }
   ```
2. Restart the app — no code changes needed.

The `Azure.Monitor.OpenTelemetry.Exporter` package is already referenced and the exporter
is registered in `TelemetryConfiguration.cs`.

---

## Running Tests

```bash
dotnet test MultiAgentApp.slnx
```

Tests cover:
- `ChatClientFactory` – client creation for both backends
- `WeatherAgent` / `ProductsAgent` – specialist graph nodes with MCP tool bindings
- `WeatherTools` / `ProductsTools` – MCP server tool logic (no network required)
- Configuration options classes

---

## Solution Structure

```
maf-msf-local-POC/
├── MultiAgentApp.slnx
├── src/
│   ├── MultiAgentApp/
│   │   ├── Agents/
│   │   │   ├── ChatClientFactory.cs   ← IChatClient factory (Foundry Local ↔ Microsoft Foundry)
│   │   │   ├── WeatherAgent.cs        ← Weather specialist graph node + MCP tools
│   │   │   ├── ProductsAgent.cs       ← Products specialist graph node + MCP tools
│   │   │   └── OrchestratorAgent.cs   ← Router/specialist/synthesizer graph flow
│   │   ├── Configuration/
│   │   │   ├── AIOptions.cs
│   │   │   ├── McpOptions.cs
│   │   │   └── TelemetryOptions.cs
│   │   ├── MCP/
│   │   │   └── McpClientFactory.cs    ← Connects to MCP servers, returns AITool[]
│   │   ├── Telemetry/
│   │   │   └── TelemetryConfiguration.cs ← OpenTelemetry + AppInsights-ready
│   │   ├── appsettings.json
│   │   └── Program.cs
│   ├── WeatherMcpServer/
│   │   └── Tools/WeatherTools.cs      ← Mocked weather tool implementations
│   └── ProductsMcpServer/
│       └── Tools/ProductsTools.cs     ← Mocked products tool implementations
└── tests/
    └── MultiAgentApp.Tests/
```
