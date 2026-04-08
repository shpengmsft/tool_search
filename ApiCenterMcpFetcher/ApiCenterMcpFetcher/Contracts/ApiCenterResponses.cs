using Newtonsoft.Json;

namespace ApiCenterMcpFetcher.Contracts;

/// <summary>
/// Paginated response for listing APIs.
/// </summary>
public class PaginatedApiResponse
{
    [JsonProperty(PropertyName = "value")]
    public List<ApiDetail> Value { get; set; } = new();

    [JsonProperty(PropertyName = "nextLink")]
    public string NextLink { get; set; }
}

/// <summary>
/// Response for listing MCP servers.
/// </summary>
public class ListMcpServersResponse
{
    [JsonProperty(PropertyName = "servers")]
    public List<McpServerContractDetails> Servers { get; set; } = new();

    [JsonProperty(PropertyName = "total_count")]
    public int TotalCount { get; set; }

    [JsonProperty(PropertyName = "next")]
    public string NextLink { get; set; }
}
