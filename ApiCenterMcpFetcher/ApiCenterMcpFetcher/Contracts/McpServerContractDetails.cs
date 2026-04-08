using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApiCenterMcpFetcher.Contracts;

public class McpServerContractDetails : McpServerContract
{
    [JsonProperty(PropertyName = "remotes", Order = 2)]
    public McpRemoteContract[] Remotes { get; set; }

    [JsonExtensionData]
    public JObject CustomProperties { get; set; }
}
