# ApiCenterIndexUploader

A standalone C# console app that simulates the **AgentAsset → Index Service** upload process. It takes the `ApiCenterToolEntities.json` output from [ApiCenterMcpFetcher](../ApiCenterMcpFetcher/) and uploads it into an Azure AI Search index with the exact same schema used by Vienna's Index Service.

## What This Program Does

```
┌──────────────────────────────────────────────┐
│  ApiCenterToolEntities.json                  │
│  (output from ApiCenterMcpFetcher)           │
│  1,515 ApiCenterToolVersionedIndexEntity     │
└──────────────────┬───────────────────────────┘
                   │
          ┌────────┴────────────────────────┐
          │  Step 1: Read JSON entities      │
          └────────┬────────────────────────┘
                   │
          ┌────────┴────────────────────────┐
          │  Step 2: Create AI Search index  │
          │  with Vienna's exact schema      │
          │  (SearchDocument_2020_02_02)     │
          │  + custom edge-ngram analyzers   │
          │  + semantic config               │
          └────────┬────────────────────────┘
                   │
          ┌────────┴────────────────────────┐
          │  Step 3: Convert entities to     │
          │  flat search documents           │
          │  (entity → SearchDocument)       │
          └────────┬────────────────────────┘
                   │
          ┌────────┴────────────────────────┐
          │  Step 4: Batch upload            │
          │  (MergeOrUpload, 1000/batch)     │
          └────────┬────────────────────────┘
                   │
          ┌────────┴────────────────────────┐
          │  Azure AI Search Index           │
          │  "azureml-tools-v0-9"            │
          └─────────────────────────────────┘
```

## How It Maps to Vienna Source Code

| This Project | Vienna Equivalent |
|---|---|
| `SearchDocument.cs` | `Index/Services.AzureSearch/Schemas/SearchDocument_2020_02_02.cs` |
| `IndexManager.cs` (index creation) | `Index/Services.AzureSearch/SearchSchemaProvider.cs` |
| `IndexManager.cs` (custom analyzers) | `Index/Services.AzureSearch/CustomAnalyzersProvider.cs` |
| `EntityToDocumentConverter.cs` | `Index/Services.AzureSearch/IndexEntityToSearchDocumentConverter.cs` |
| `InputContracts.cs` | `AgentAsset/Contracts/ApiCenter/ApiCenterIndexEntity.cs` |
| `Program.cs` (batch upload) | `Index/Services.AzureSearch/AzureSearchShard.cs` → MergeOrUpload |

## Index Schema

The index is created with all fields from Vienna's `SearchDocument_2020_02_02`:

| Field | Type | Searchable | Filterable | Sortable | Notes |
|-------|------|:----------:|:----------:|:--------:|-------|
| `Id` | string | | | | Document key (base64 of entity ID) |
| `Type` | string | ✅ | ✅ | ✅ | Entity type = `"tools"` |
| `Kind` | string | ✅ | ✅ | ✅ | e.g., `"mcp"` |
| `Name` | string | ✅ | ✅ | ✅ | Tool/API name (objectId) |
| `EntityContainerId` | string | ✅ | ✅ | | API Center name |
| `EntityContainerIdToLower` | string | ✅ | ✅ | | API Center name (lowercase) |
| `Version` | string | ✅ | ✅ | ✅ | Version = `"1"` |
| `EntityObjectId` | string | ✅ | ✅ | ✅ | Same as Name |
| `ResourceType` | string | | ✅ | | = `"ApiCenter"` |
| `EntityId` | string | ✅ | ✅ | | Full entity ID string |
| `Labels` | string[] | ✅ | ✅ | | e.g., `["latest"]` |
| `Usage` | complex | | ✅ | ✅ | `{ Popularity, TotalCount }` |
| `CreatedTime` | long | | ✅ | ✅ | Unix timestamp (ms) |
| `UpdatedTime` | long | | ✅ | ✅ | Unix timestamp (ms) |
| `AnnotationsSerialized` | string | ✅ | | | JSON blob: description, tags |
| `PropertiesSerialized` | string | ✅ | | | JSON blob: title, kind, remotes, etc. |
| `AllStringFieldValuesSerialized` | string | ✅ | | | Flattened text for ngram prefix search |
| `StringAnnotations` | complex[] | ✅ | ✅ | | NameValuePair list for non-schema annotations |
| `StringProperties` | complex[] | ✅ | ✅ | | NameValuePair list for non-schema properties |
| `SearchableTags` | complex[] | ✅ | ✅ | | Tag name-value pairs |
| `ShardingTenantId` | string | | ✅ | | Tenant isolation |
| `ResourceShardingNumber` | int | | ✅ | | Shard routing |

### Custom Analyzers (same as Vienna's `CustomAnalyzersProvider`)

**`prefix-ngram-analyzer`** (index analyzer for `AllStringFieldValuesSerialized`):
- Tokenizer: Standard
- Token filters: Lowercase → KStem → Stopwords → EdgeNGram (min=2, max=50, front)

**`prefix-search-analyzer`** (search analyzer for `AllStringFieldValuesSerialized`):
- Tokenizer: Standard
- Token filters: Lowercase → KStem → Stopwords

### Semantic Configuration

```
Config name:    "tools-semantic-config"
Title field:    Name
Content fields: AnnotationsSerialized, AllStringFieldValuesSerialized
Keywords:       Labels
```

## Entity-to-Document Conversion

The converter (`EntityToDocumentConverter.cs`) replicates Vienna's `IndexEntityToSearchDocumentConverter`:

```
ApiCenterToolVersionedIndexEntity
│
├── EntityId          → Id (base64), Name, EntityContainerId, Version, EntityObjectId, ResourceType, EntityId
├── Usage             → Usage { Popularity, TotalCount }
├── Annotations
│   ├── Description   → AnnotationsSerialized (JSON), AllStringFieldValuesSerialized
│   ├── Tags          → SearchableTags (NameTagPair list)
│   └── ExtensionData → StringAnnotations / DateTimeOffsetAnnotations (NameValuePair lists)
│
└── Properties
    ├── Title, Kind   → AnnotationsSerialized, AllStringFieldValuesSerialized
    ├── UpdatedTime   → UpdatedTime (unix ms)
    ├── CreationContext → CreatedTime (unix ms)
    └── ExtensionData → StringProperties / DoubleProperties / DateTimeOffsetProperties
```

**Key behaviors replicated:**
- `AnnotationsSerialized` / `PropertiesSerialized` — full JSON serialization of annotations/properties
- `AllStringFieldValuesSerialized` — space-separated concatenation of all searchable text values (name, description, title, kind, tags, string extension data)
- `StringAnnotations` / `StringProperties` — non-schema fields stored as NameValuePair lists for filtering
- Tags parsed into `SearchableTags` with standard analyzer (not keyword)
- `CreatedTime` / `UpdatedTime` converted to unix milliseconds

## Usage

### Prerequisites
- .NET 10.0
- Azure AI Search instance
- Admin API key (or `Search Index Data Contributor` RBAC role)

### Run

```bash
# Set the admin key
$env:SEARCH_ADMIN_KEY = "<your-admin-key>"

# Run with default input file (ApiCenterToolEntities.json in current directory)
cd ApiCenterIndexUploader
dotnet run

# Or specify a custom input file path
dotnet run -- "Q:\shpengmsft\tool_search\ApiCenterMcpFetcher\ApiCenterToolEntities.json"
```

### Configuration

Edit `Program.cs` to change:
- `searchEndpoint` — your AI Search endpoint URL
- `indexName` — target index name (default: `azureml-tools-v0-9`)

### End-to-End Pipeline

```bash
# Step 1: Fetch MCP data from API Center → JSON
cd Q:\shpengmsft\tool_search\ApiCenterMcpFetcher
dotnet run
# Output: ApiCenterToolEntities.json (1,515 entities)

# Step 2: Upload JSON → AI Search index
cd Q:\shpengmsft\tool_search\ApiCenterIndexUploader
$env:SEARCH_ADMIN_KEY = "<key>"
dotnet run -- "..\ApiCenterMcpFetcher\ApiCenterToolEntities.json"
# Creates index + uploads 1,515 documents
```

## Dependencies

- .NET 10.0
- Azure.Search.Documents 11.7.0
- Azure.Identity
- Newtonsoft.Json 13.0.3
