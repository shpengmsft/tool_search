using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApiCenterIndexUploader;

/// <summary>
/// Input entity structure — matches the JSON output from ApiCenterMcpFetcher.
/// </summary>
public class ApiCenterToolEntity
{
    public EntityIdContract EntityId { get; set; }
    public long UpdateSequence { get; set; }
    public string Version { get; set; }
    public string Type { get; set; }
    public EntityUsageContract Usage { get; set; }
    public AnnotationsContract Annotations { get; set; }
    public JObject Properties { get; set; }
}

public class EntityIdContract
{
    public string Region { get; set; }
    public string EntityContainerId { get; set; }
    public string EntityType { get; set; }
    public string ObjectId { get; set; }
    public string ResourceType { get; set; }
    public string Version { get; set; }

    public string ToEntityIdString() =>
        $"azureml://location/{Region}/apiCenter/{EntityContainerId}/type/{EntityType}/objectId/{ObjectId}/version/{Version}";
}

public class EntityUsageContract
{
    public double Popularity { get; set; }
}

public class AnnotationsContract
{
    public Dictionary<string, string> Tags { get; set; }
    public string Description { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JToken> ExtensionData { get; set; }
}
