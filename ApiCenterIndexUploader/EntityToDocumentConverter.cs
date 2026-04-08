using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ApiCenterIndexUploader;

/// <summary>
/// Converts ApiCenterToolVersionedIndexEntity → SearchDocument_2020_02_02 format.
/// Replicates: Index/Services.AzureSearch/IndexEntityToSearchDocumentConverter.cs
/// </summary>
public static class EntityToDocumentConverter
{
    private static readonly JsonSerializer CamelSerializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    });

    /// <summary>
    /// Generates a deterministic document ID from entity fields.
    /// Vienna uses a hash-based ID construction.
    /// </summary>
    private static string GenerateDocumentId(ApiCenterToolEntity entity)
    {
        var raw = $"{entity.EntityId.Region}_{entity.EntityId.EntityContainerId}_{entity.EntityId.ObjectId}_{entity.Version}";
        return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public static SearchDocument ConvertToDocument(ApiCenterToolEntity entity, string shardingTenantId = "00000000-0000-0000-0000-000000000000")
    {
        var eid = entity.EntityId;
        var annotations = entity.Annotations;
        var properties = entity.Properties;

        // Serialize annotations and properties as JSON strings (same as Vienna)
        var annotationsSerialized = JsonConvert.SerializeObject(annotations, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        });

        var propertiesSerialized = JsonConvert.SerializeObject(properties, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        });

        // Build AllStringFieldValuesSerialized — space-separated searchable text
        var allStrings = new List<string>();
        AddIfNotEmpty(allStrings, eid.ObjectId);
        AddIfNotEmpty(allStrings, annotations?.Description);
        if (properties != null)
        {
            AddIfNotEmpty(allStrings, properties.Value<string>("title"));
            AddIfNotEmpty(allStrings, properties.Value<string>("kind"));
        }
        if (annotations?.Tags != null)
        {
            foreach (var tag in annotations.Tags)
            {
                AddIfNotEmpty(allStrings, tag.Key);
                AddIfNotEmpty(allStrings, tag.Value);
            }
        }
        // Flatten extension data strings
        if (annotations?.ExtensionData != null)
        {
            foreach (var kvp in annotations.ExtensionData)
            {
                if (kvp.Value?.Type == JTokenType.String)
                    AddIfNotEmpty(allStrings, kvp.Value.ToString());
            }
        }

        // Extract typed annotation fields not in schema → NameValuePair lists
        var stringAnnotations = new List<NameValuePairString>();
        var dateTimeAnnotations = new List<NameValuePairDateTimeOffset>();
        if (annotations?.ExtensionData != null)
        {
            foreach (var kvp in annotations.ExtensionData)
            {
                if (kvp.Value == null) continue;
                if (kvp.Value.Type == JTokenType.Date || DateTimeOffset.TryParse(kvp.Value.ToString(), out _))
                {
                    if (DateTimeOffset.TryParse(kvp.Value.ToString(), out var dt))
                        dateTimeAnnotations.Add(new NameValuePairDateTimeOffset { Name = kvp.Key, Value = dt });
                }
                else if (kvp.Value.Type == JTokenType.String)
                {
                    stringAnnotations.Add(new NameValuePairString { Name = kvp.Key, Value = kvp.Value.ToString() });
                }
            }
        }

        // Extract typed property fields not in top-level schema → NameValuePair lists
        var stringProperties = new List<NameValuePairString>();
        var dateTimeProperties = new List<NameValuePairDateTimeOffset>();
        var doubleProperties = new List<NameValuePairDouble>();
        // top-level schema fields that are handled separately
        var schemaFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "title", "kind", "entityArmId", "xMsLicense", "xMsAuthSchemas", "xMsSecuritySchemes",
            "tags", "updatedTime", "creationContext"
        };
        if (properties != null)
        {
            foreach (var prop in properties)
            {
                if (schemaFields.Contains(prop.Key)) continue;
                if (prop.Value == null || prop.Value.Type == JTokenType.Null) continue;

                if (prop.Value.Type == JTokenType.String)
                {
                    stringProperties.Add(new NameValuePairString { Name = prop.Key, Value = prop.Value.ToString() });
                    AddIfNotEmpty(allStrings, prop.Value.ToString());
                }
                else if (prop.Value.Type == JTokenType.Float || prop.Value.Type == JTokenType.Integer)
                {
                    doubleProperties.Add(new NameValuePairDouble { Name = prop.Key, Value = prop.Value.ToObject<double>() });
                }
                else if (prop.Value.Type == JTokenType.Date)
                {
                    dateTimeProperties.Add(new NameValuePairDateTimeOffset { Name = prop.Key, Value = prop.Value.ToObject<DateTimeOffset>() });
                }
            }
        }

        // Extract CreatedTime and UpdatedTime as unix timestamps (ms)
        long? createdTimeMs = null;
        long? updatedTimeMs = null;
        if (properties != null)
        {
            var creationCtx = properties.Value<JObject>("creationContext");
            if (creationCtx != null)
            {
                var ct = creationCtx.Value<string>("createdTime");
                if (DateTimeOffset.TryParse(ct, out var createdDt))
                    createdTimeMs = createdDt.ToUnixTimeMilliseconds();
            }

            var ut = properties.Value<string>("updatedTime");
            if (DateTimeOffset.TryParse(ut, out var updatedDt))
                updatedTimeMs = updatedDt.ToUnixTimeMilliseconds();
        }

        // Build searchable tags
        var searchableTags = new List<NameTagPair>();
        if (annotations?.Tags != null)
        {
            foreach (var tag in annotations.Tags)
                searchableTags.Add(new NameTagPair { Name = tag.Key, Value = tag.Value });
        }

        return new SearchDocument
        {
            Id = GenerateDocumentId(entity),
            Type = entity.Type,
            Kind = properties?.Value<string>("kind"),
            Name = eid.ObjectId,
            EntityContainerId = eid.EntityContainerId,
            EntityContainerIdToLower = eid.EntityContainerId?.ToLower(),
            Version = entity.Version,
            EntityObjectId = eid.ObjectId,
            ResourceType = eid.ResourceType,
            EntityId = eid.ToEntityIdString(),
            UpdateSequence = entity.UpdateSequence,
            ShardingTenantId = shardingTenantId,
            ResourceTenantId = shardingTenantId,
            ResourceShardingNumber = 0,
            Labels = new List<string> { "latest" },
            Usage = new UsageInternal
            {
                Popularity = entity.Usage?.Popularity,
                TotalCount = 0
            },
            CreatedTime = createdTimeMs,
            UpdatedTime = updatedTimeMs,
            LastRebuildUnixTimestampInMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            AnnotationsSerialized = annotationsSerialized,
            PropertiesSerialized = propertiesSerialized,
            AllStringFieldValuesSerialized = string.Join(" ", allStrings),
            StringAnnotations = stringAnnotations,
            StringProperties = stringProperties,
            DoubleAnnotations = new List<NameValuePairDouble>(),
            DoubleProperties = doubleProperties,
            DateTimeOffsetAnnotations = dateTimeAnnotations,
            DateTimeOffsetProperties = dateTimeProperties,
            StringTags = new List<NameKeywordPair>(),
            SearchableTags = searchableTags,
            Relationships = new List<RelationshipImpl>(),
        };
    }

    private static void AddIfNotEmpty(List<string> list, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            list.Add(value);
    }
}
