using Newtonsoft.Json;

namespace ApiCenterMcpFetcher.Contracts;

public class McpServerContract
{
    [JsonProperty(PropertyName = "id", Order = -5)]
    public Guid Id { get; set; }

    [JsonProperty(PropertyName = "name", Order = -4)]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "description", Order = -3)]
    public string Description { get; set; }

    [JsonProperty(PropertyName = "repository", Order = -2)]
    public McpServerRepository Repository { get; set; }

    [JsonProperty(PropertyName = "version_detail", Order = -2)]
    public McpServerVersionDetail VersionDetail { get; set; }
}
