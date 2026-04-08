using System.Web;
using ApiCenterMcpFetcher.Contracts;
using Newtonsoft.Json;

namespace ApiCenterMcpFetcher;

/// <summary>
/// HTTP client for Azure API Center dataplane APIs.
/// Uses the same endpoints as Vienna's IApiCenterWorkspacesDataplane interface:
///   - GET /workspaces/default/apis                  → List APIs
///   - GET /workspaces/default/apis/{apiName}        → Get API by name
///   - GET /workspaces/default/v0/servers             → List MCP servers
///   - GET /workspaces/default/v0/servers/{apiName}   → Get MCP server by name
/// 
/// Public API Centers use Authentication=None, so no auth headers needed.
/// </summary>
public class ApiCenterClient
{
    private readonly HttpClient _httpClient;
    private const string SchemaVersion = "2025-06-01";
    private const string ApiCenterSuffix = "azure-apicenter.ms";

    public ApiCenterClient()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    private string GetBaseUrl(string apiCenterName, string location) =>
        $"https://{apiCenterName}.data.{location}.{ApiCenterSuffix}";

    // ── List all APIs (with pagination) ──
    // Same as Vienna: ApiCenterWorkspacesDataplaneClient.GetAllApis()
    public async Task<List<ApiDetail>> GetAllApis(string apiCenterName, string location)
    {
        var baseUrl = GetBaseUrl(apiCenterName, location);
        var allApis = new List<ApiDetail>();
        string skipToken = null;

        do
        {
            var url = $"{baseUrl}/workspaces/default/apis?schema-version={SchemaVersion}";
            if (!string.IsNullOrEmpty(skipToken))
                url += $"&$skiptoken={Uri.EscapeDataString(skipToken)}";

            var json = await _httpClient.GetStringAsync(url);
            var result = JsonConvert.DeserializeObject<PaginatedApiResponse>(json);
            if (result?.Value != null)
                allApis.AddRange(result.Value);

            // Handle pagination
            skipToken = null;
            if (!string.IsNullOrEmpty(result?.NextLink))
            {
                var query = HttpUtility.ParseQueryString(new Uri(result.NextLink).Query);
                skipToken = query["$skiptoken"];
            }
        }
        while (!string.IsNullOrEmpty(skipToken));

        return allApis;
    }

    // ── Get single API by name ──
    // Same as Vienna: IApiCenterWorkspacesDataplane.GetApiByName()
    public async Task<ApiDetail> GetApiByName(string apiCenterName, string location, string apiName)
    {
        var url = $"{GetBaseUrl(apiCenterName, location)}/workspaces/default/apis/{apiName}?schema-version={SchemaVersion}";
        var json = await _httpClient.GetStringAsync(url);
        return JsonConvert.DeserializeObject<ApiDetail>(json);
    }

    // ── List all MCP servers (with pagination) ──
    // Same as Vienna: ApiCenterWorkspacesDataplaneClient.GetAllMcpServers()
    public async Task<List<McpServerContractDetails>> GetAllMcpServers(string apiCenterName, string location)
    {
        var baseUrl = GetBaseUrl(apiCenterName, location);
        var allServers = new List<McpServerContractDetails>();
        string skipToken = null;
        string limit = null;

        do
        {
            var url = $"{baseUrl}/workspaces/default/v0/servers?schema-version={SchemaVersion}";
            if (!string.IsNullOrEmpty(skipToken))
                url += $"&$skiptoken={Uri.EscapeDataString(skipToken)}";
            if (!string.IsNullOrEmpty(limit))
                url += $"&limit={limit}";

            var json = await _httpClient.GetStringAsync(url);
            var result = JsonConvert.DeserializeObject<ListMcpServersResponse>(json);
            if (result?.Servers != null)
                allServers.AddRange(result.Servers);

            // Handle pagination
            skipToken = null;
            limit = null;
            if (!string.IsNullOrEmpty(result?.NextLink))
            {
                var query = HttpUtility.ParseQueryString(new Uri(result.NextLink).Query);
                skipToken = query["$skiptoken"];
                limit = query["limit"];
            }
        }
        while (!string.IsNullOrEmpty(skipToken));

        return allServers;
    }

    // ── Get MCP server by API name ──
    // Same as Vienna: IApiCenterWorkspacesDataplane.GetMcpServerByName()
    public async Task<McpServerContractDetails> GetMcpServerByName(
        string apiCenterName, string location, string apiName)
    {
        var url = $"{GetBaseUrl(apiCenterName, location)}/workspaces/default/v0/servers/{apiName}?schema-version={SchemaVersion}";
        var json = await _httpClient.GetStringAsync(url);
        return JsonConvert.DeserializeObject<McpServerContractDetails>(json);
    }

    // ── Match APIs to MCP servers (one-by-one lookup) ──
    // Same as Vienna: ApiCenterWorkspacesDataplaneClient.GetMcpServersForApis()
    public async Task<Dictionary<string, McpServerContractDetails>> GetMcpServersForApis(
        string apiCenterName, string location, IList<ApiDetail> apis)
    {
        var mcpServers = new Dictionary<string, McpServerContractDetails>();

        foreach (var api in apis)
        {
            try
            {
                var mcpServer = await GetMcpServerByName(apiCenterName, location, api.Name);
                mcpServers[api.Name] = mcpServer;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Same as Vienna: skip APIs without MCP server entries
                continue;
            }
        }

        return mcpServers;
    }
}
