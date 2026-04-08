using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApiCenterMcpFetcher.Contracts;

public class ApiDetail
{
    public string Name { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Summary { get; set; }
    public string Kind { get; set; }
    public string LifecycleStage { get; set; }
    public List<ExternalDocumentation> ExternalDocumentation { get; set; }
    public JArray Contacts { get; set; }
    public CustomProperties CustomProperties { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
}

public class ExternalDocumentation
{
    public string Url { get; set; }
    public string Title { get; set; }
}

public class CustomProperties
{
    public string Vendor { get; set; }
    public string Endpoint { get; set; }
    public string Visibility { get; set; }
    public string Type { get; set; }
    public string Icon { get; set; }
    public string Categories { get; set; }

    [JsonProperty(PropertyName = "x-ms-connector-name")]
    public string XMsConnectorName { get; set; }
}
