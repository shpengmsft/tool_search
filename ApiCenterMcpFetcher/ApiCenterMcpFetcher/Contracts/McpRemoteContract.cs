using Newtonsoft.Json;

namespace ApiCenterMcpFetcher.Contracts;

public class McpRemoteContract
{
    [JsonProperty(PropertyName = "transport_type")]
    public TransportType TransportType { get; set; }

    [JsonProperty(PropertyName = "url")]
    public string Url { get; set; }
}
