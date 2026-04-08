# ApiCenterMcpFetcher

A standalone C# console app that replicates Vienna's AgentAsset data ingestion pipeline — fetching MCP (Model Context Protocol) server metadata from Azure API Center and transforming it into the same `ApiCenterToolVersionedIndexEntity` structure used by the Index Service.

## What This Program Does

```
┌─────────────────────────────────────────────────────┐
│  Azure API Center (Public, No Auth)                 │
│  ├── registry-prod-bl          (71 MCP servers)     │
│  └── connectors-registry-prod-bl (1,444 connectors) │
└──────────────────┬──────────────────────────────────┘
                   │
          ┌────────┴────────┐
          │  1. Fetch APIs  │  GET /workspaces/default/apis
          │  2. Fetch MCP   │  GET /workspaces/default/v0/servers/{name}
          └────────┬────────┘
                   │
          ┌────────┴──────────────────────────┐
          │  3. Transform                      │
          │  ApiDetail + McpServerDetails      │
          │  → ApiCenterToolVersionedIndexEntity│
          └────────┬──────────────────────────┘
                   │
          ┌────────┴────────┐
          │  4. Save JSON   │  ApiCenterToolEntities.json
          └─────────────────┘
```

## How It Maps to Vienna Source Code

This program replicates the exact logic from the Vienna repo (`src/azureml-api/src/`):

| This Project | Vienna Equivalent |
|---|---|
| `ApiCenterClient.cs` | `AgentAsset/Services/ApiCenter/ApiCenterWorkspacesDataplaneClient.cs` |
| `ApiCenterIndexServiceUtilities.cs` | `AgentAsset/Services/ApiCenter/ApiCenterIndexServiceUtilities.cs` |
| `Contracts/ApiContracts.cs` | `AgentAsset/Contracts/ApiCenter/ApiContracts.cs` |
| `Contracts/McpServerContractDetails.cs` | `AgentAsset/Contracts/ApiCenter/McpRegistry/McpServerContractDetails.cs` |
| `Contracts/McpServerContract.cs` | `AgentAsset/Contracts/ApiCenter/McpRegistry/McpServerContract.cs` |
| `Contracts/IndexEntity.cs` | `AgentAsset/Contracts/ApiCenter/ApiCenterIndexEntity.cs` + `Index/Contracts/EntityTypes.cs` |
| `Program.cs` | `AgentAsset/Services/ApiCenter/ApiCenterIndexService.cs` → `IndexPublicApiCenter()` |

## Step-by-Step Logic

### Step 1 — Fetch All APIs from API Center

**Vienna method:** `ApiCenterWorkspacesDataplaneClient.GetAllApis()`

```
GET https://{apiCenterName}.data.eastus.azure-apicenter.ms/workspaces/default/apis?schema-version=2025-06-01
```

Returns a paginated list of `ApiDetail` objects:
- `Name` — API identifier (e.g., `"github-mcp-server"`)
- `Title` — Display name (e.g., `"GitHub"`)
- `Description`, `Kind`, `LifecycleStage`, `CustomProperties`, etc.

### Step 2 — Fetch MCP Server Details per API

**Vienna method:** `ApiCenterWorkspacesDataplaneClient.GetMcpServersForApis()`

For each API, makes a call:
```
GET https://{apiCenterName}.data.eastus.azure-apicenter.ms/workspaces/default/v0/servers/{apiName}?schema-version=2025-06-01
```

Returns `McpServerContractDetails`:
- `Name`, `Description` — Server-level metadata
- `Repository` — Git repo URL
- `VersionDetail` — Version, release date
- `Remotes[]` — Transport endpoints (`{ transport_type: "sse", url: "https://..." }`)
- `CustomProperties` — Catch-all for `x-ms-license`, `x-ms-auth-schemas`, `x-ms-security-schemes`, tags, etc.

APIs without an MCP server entry return 404 and are skipped (same as Vienna).

### Step 3 — Transform to Index Entity

**Vienna method:** `ApiCenterIndexServiceUtilities.ConstructApiCenterToolVersionedIndexEntity()`

Merges both data sources into a single `ApiCenterToolVersionedIndexEntity`:

```
ApiCenterToolVersionedIndexEntity
├── EntityId
│   ├── Region: "eastus"
│   ├── EntityContainerId: API Center name
│   ├── EntityType: "tools"
│   ├── ObjectId: API name
│   ├── ResourceType: "ApiCenter"
│   └── Version: "1"
│
├── Usage.Popularity: 300 (default) or 500 (built-in)
│
├── Annotations
│   ├── Description: from McpServerContractDetails     ← SEARCHABLE
│   ├── Tags: extracted from CustomProperties          ← FILTERABLE
│   └── lastUpdated: from ApiDetail
│
└── Properties
    ├── Title: from ApiDetail                          ← SEARCHABLE
    ├── Kind: from ApiDetail                           ← FILTERABLE
    ├── EntityArmId: ARM resource ID
    ├── XMsLicense: from CustomProperties
    ├── XMsAuthSchemas: from CustomProperties
    ├── XMsSecuritySchemes: derived (oauth2/apikey/unauthenticated/managedidentity)
    ├── Tags: tag keys
    ├── UpdatedTime: from ApiDetail
    ├── CreationContext: { CreatedTime, CreatedBy }
    └── ExtensionData (stored, not searchable):
        ├── versionDetail, remotes, kind, lifecycleStage
        ├── externalDocumentation, contacts, customProperties
        └── all MCP server custom properties appended
```

**Key transformation details:**
- **Description** comes from the MCP server, not the API
- **Tool type** is classified as: `built-in` → `connector` → `remotes` → `local`
- **Security schemes** are derived from `x-ms-security-schemes` with Entra domain detection
- **Popularity** defaults to 300, built-in tools get 500
- **MCP custom properties** are flattened into Properties.ExtensionData

### Step 4 — Save to JSON

All entities are serialized to `ApiCenterToolEntities.json` in the current directory.

In Vienna, this step would instead be:
```csharp
await _indexClient.UpdateVersionedEntitiesWithSchema(entityUpdateMessage);
```
which batch-pushes to the Index Service via S2S, writing to Azure AI Search index `azureml-v0-9`.

## Public API Centers

Two Microsoft-managed API Centers are configured (hardcoded defaults from `AgentAssetServiceConfiguration.cs`):

| Name | Data API Hostname | Content |
|---|---|---|
| `registry-prod-bl` | `registry-prod-bl.data.eastus.azure-apicenter.ms` | MCP servers (GitHub, Stripe, Neon, etc.) |
| `connectors-registry-prod-bl` | `connectors-registry-prod-bl.data.eastus.azure-apicenter.ms` | Power Platform connectors |

Both use `Authentication = "None"` — no auth headers needed.

## Usage

```bash
cd ApiCenterMcpFetcher
dotnet run
```

Output: `ApiCenterToolEntities.json` in the current directory (~3 MB, ~1,515 entities).

## Output Schema

Each entity in the JSON array follows this structure:

```jsonc
{
  "EntityId": {
    "Region": "eastus",
    "EntityContainerId": "registry-prod-bl",
    "EntityType": "tools",
    "ObjectId": "github-mcp-server",
    "ResourceType": "ApiCenter",
    "Version": "1"
  },
  "UpdateSequence": 1775112014,
  "Version": "1",
  "Type": "tools",
  "Usage": { "Popularity": 300.0 },
  "Annotations": {
    "Tags": {},
    "Description": "Access GitHub repositories, issues, and pull requests...",
    "lastUpdated": "2026-04-01T..."
  },
  "Properties": {
    "Title": "GitHub",
    "Kind": "mcp",
    "EntityArmId": "/subscriptions/.../services/registry-prod-bl",
    "XMsSecuritySchemes": ["apikey"],
    "Tags": [],
    "UpdatedTime": "2026-04-01T...",
    "versionDetail": { "version": "Original", "is_latest": true },
    "remotes": [{ "transport_type": "sse", "url": "https://api.githubcopilot.com/mcp" }],
    "customProperties": { "vendor": "1P", "type": "remotes", "icon": "..." }
  }
}
```

