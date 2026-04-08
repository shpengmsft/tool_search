using Newtonsoft.Json;

namespace ApiCenterMcpFetcher.Contracts;

public class McpServerRepository
{
    [JsonProperty(PropertyName = "url")]
    public string Url { get; set; }

    [JsonProperty(PropertyName = "source")]
    public string Source { get; set; }

    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
}
