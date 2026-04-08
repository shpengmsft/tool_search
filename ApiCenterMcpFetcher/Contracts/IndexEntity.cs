using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApiCenterMcpFetcher.Contracts;

// ── Simplified Vienna Index contracts ──
// Mirrors: VersionedEntity, EntityId, EntityType, etc.

public class EntityId
{
    public string Region { get; set; }
    public string EntityContainerId { get; set; }
    public string EntityType { get; set; }
    public string ObjectId { get; set; }
    public string ResourceType { get; set; }
    public string Version { get; set; }

    public EntityId() { }

    public EntityId(string region, string entityContainerId, string type, string objectId, string resourceType, string version = null)
    {
        Region = region;
        EntityContainerId = entityContainerId;
        EntityType = type;
        ObjectId = objectId;
        ResourceType = resourceType;
        Version = version;
    }

    public override string ToString() =>
        $"azureml://location/{Region}/apiCenter/{EntityContainerId}/type/{EntityType}/objectId/{ObjectId}/version/{Version}";
}

public static class EntityType
{
    public const string AgentTools = "tools";
}

public static class EntityContainerType
{
    public const string ApiCenter = "ApiCenter";
}

public class EntityUsage
{
    public double Popularity { get; set; }
}

public class CreatedBy { }

public class CreationContext
{
    public DateTimeOffset CreatedTime { get; set; }
    public CreatedBy CreatedBy { get; set; } = new();
}

// ── Versioned Annotations ──
public class ApiCenterToolVersionedAnnotations
{
    public Dictionary<string, string> Tags { get; set; }
    public string Description { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JToken> ExtensionData { get; set; }
}

// ── Versioned Properties ──
public class ApiCenterToolVersionedProperties
{
    public string Title { get; set; }
    public string Kind { get; set; }
    public string EntityArmId { get; set; }
    public string XMsLicense { get; set; }
    public IList<string> XMsAuthSchemas { get; set; }
    public IList<string> XMsSecuritySchemes { get; set; }
    public IList<string> Tags { get; set; }
    public DateTimeOffset? UpdatedTime { get; set; }
    public CreationContext CreationContext { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JToken> ExtensionData { get; set; }
}

// ── The main entity (what gets sent to Index Service) ──
public class ApiCenterToolVersionedIndexEntity
{
    public EntityId EntityId { get; set; }
    public long UpdateSequence { get; set; }
    public string Version { get; set; }
    public string Type { get; set; }
    public EntityUsage Usage { get; set; }
    public ApiCenterToolVersionedAnnotations Annotations { get; set; }
    public ApiCenterToolVersionedProperties Properties { get; set; }
}
