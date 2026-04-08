using Newtonsoft.Json;

namespace ApiCenterMcpFetcher.Contracts;

public class McpServerVersionDetail
{
    [JsonProperty(PropertyName = "version")]
    public string Version { get; set; }

    [JsonProperty(PropertyName = "release_date")]
    public DateTime? ReleaseDate { get; set; }

    [JsonProperty(PropertyName = "is_latest", DefaultValueHandling = DefaultValueHandling.Populate)]
    public bool? IsLatest { get; set; }
}
