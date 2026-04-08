using ApiCenterMcpFetcher.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ApiCenterMcpFetcher;

/// <summary>
/// Replicates Vienna's ApiCenterIndexServiceUtilities.ConstructApiCenterToolVersionedIndexEntity()
/// from: src/azureml-api/src/AgentAsset/Services/ApiCenter/ApiCenterIndexServiceUtilities.cs
/// </summary>
public class ApiCenterIndexServiceUtilities
{
    private const double DefaultApiCenterToolsPopularity = 300;
    private const double BuiltInApiCenterToolsPopularity = 500;
    private const string PublicConnectorsRegistryApiCenterName = "connectors-registry-prod-bl";

    private static readonly List<string> WellKnownEntraAuthorizationDomains = new()
    {
        "https://enterpriseregistration.windows.net",
        "https://login.microsoftonline.com",
        "https://login.microsoft.com",
        "https://sts.windows.net",
    };

    private static readonly JsonSerializer JSerializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    });

    public ApiCenterToolVersionedIndexEntity ConstructApiCenterToolVersionedIndexEntity(
        string resourceLocation,
        string apiCenterName,
        McpServerContractDetails mcpServerContractDetails,
        string version,
        ApiDetail apiDetail,
        string apiCenterArmScope)
    {
        var entityId = new EntityId(
            region: resourceLocation,
            entityContainerId: apiCenterName,
            type: EntityType.AgentTools,
            objectId: apiDetail.Name,
            resourceType: EntityContainerType.ApiCenter,
            version: version);

        var updateSequence = apiDetail.LastUpdated.HasValue
            ? apiDetail.LastUpdated.Value.ToUnixTimeSeconds()
            : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string xMsLicense = ExtractXMsLicenseFromCustomProperties(mcpServerContractDetails.CustomProperties);
        string xMsConnectorName = ExtractXMsConnectorNameFromCustomProperties(mcpServerContractDetails.CustomProperties);
        List<string> xMsAuthSchemas = ExtractXMsAuthSchemasFromCustomProperties(mcpServerContractDetails.CustomProperties);
        List<string> xMsSecuritySchemes = ExtractXMsSecuritySchemesFromCustomProperties(mcpServerContractDetails.CustomProperties, xMsAuthSchemas);
        Dictionary<string, string> tags = ExtractTagsFromCustomProperties(mcpServerContractDetails.CustomProperties);
        string apiCenterToolType = ExtractApiCenterToolTypeFromCustomProperties(mcpServerContractDetails, xMsConnectorName, apiCenterName, tags);
        JArray contacts = ExtractContactsFromCustomProperties(apiDetail, mcpServerContractDetails.CustomProperties);
        var popularity = SetPopularityForApiCenterTool(apiCenterName, entityId.ToString(), apiCenterToolType);

        var apiCustomProperties = apiDetail.CustomProperties ?? new CustomProperties();
        apiCustomProperties.Type = apiCenterToolType;
        apiCustomProperties.XMsConnectorName = apiCustomProperties.XMsConnectorName ?? xMsConnectorName;

        var createdTime = DateTimeOffset.UtcNow;
        if (mcpServerContractDetails.VersionDetail != null && mcpServerContractDetails.VersionDetail.ReleaseDate != default)
        {
            createdTime = mcpServerContractDetails.VersionDetail.ReleaseDate.Value;
        }
        else if (mcpServerContractDetails.CustomProperties != null &&
                 mcpServerContractDetails.CustomProperties.TryGetValue("created_at", out var createdTimeJToken))
        {
            if (DateTimeOffset.TryParse(createdTimeJToken?.ToString(), out var parsedCreatedTime))
            {
                createdTime = parsedCreatedTime;
            }
        }

        var apiCenterIndexEntity = new ApiCenterToolVersionedIndexEntity
        {
            EntityId = entityId,
            UpdateSequence = updateSequence,
            Version = version,
            Type = entityId.EntityType,
            Usage = new EntityUsage
            {
                Popularity = popularity
            },
            Annotations = new ApiCenterToolVersionedAnnotations
            {
                Tags = tags,
                Description = mcpServerContractDetails.Description,
                ExtensionData = new Dictionary<string, JToken>
                {
                    ["lastUpdated"] = apiDetail.LastUpdated ?? DateTimeOffset.Now,
                }
            },
            Properties = new ApiCenterToolVersionedProperties
            {
                Kind = apiDetail.Kind,
                EntityArmId = apiCenterArmScope,
                XMsLicense = xMsLicense,
                XMsAuthSchemas = xMsAuthSchemas,
                XMsSecuritySchemes = xMsSecuritySchemes,
                Tags = tags.Keys.ToList(),
                UpdatedTime = apiDetail.LastUpdated ?? DateTimeOffset.Now,
                Title = apiDetail.Title,
                ExtensionData = new Dictionary<string, JToken>
                {
                    ["versionDetail"] = ToJObject(mcpServerContractDetails.VersionDetail),
                    ["remotes"] = ToJArray(mcpServerContractDetails.Remotes),
                    ["kind"] = apiDetail.Kind,
                    ["lifecycleStage"] = apiDetail.LifecycleStage,
                    ["externalDocumentation"] = ToJArray(apiDetail.ExternalDocumentation),
                    ["contacts"] = contacts,
                    ["customProperties"] = ToJObject(apiCustomProperties),
                },
                CreationContext = new CreationContext
                {
                    CreatedTime = createdTime,
                    CreatedBy = new CreatedBy()
                }
            }
        };

        // Append MCP Server custom properties into ExtensionData (same as Vienna)
        if (mcpServerContractDetails.CustomProperties != null)
        {
            foreach (var customProperty in mcpServerContractDetails.CustomProperties)
            {
                var key = customProperty.Key;
                var value = customProperty.Value;
                if (value != null && !apiCenterIndexEntity.Properties.ExtensionData.ContainsKey(key))
                {
                    apiCenterIndexEntity.Properties.ExtensionData[key] = value;
                }
            }
        }

        return apiCenterIndexEntity;
    }

    // ── Helper methods (exact replicas from Vienna) ──

    private string ExtractXMsLicenseFromCustomProperties(JObject customProperties)
    {
        if (customProperties != null && customProperties.TryGetValue("x-ms-license", out var jToken))
        {
            var jObj = jToken as JObject;
            if (jObj != null && jObj.TryGetValue("name", out var name))
                return name?.ToString();
        }
        return null;
    }

    private string ExtractXMsConnectorNameFromCustomProperties(JObject customProperties)
    {
        if (customProperties != null && customProperties.TryGetValue("x-ms-connector-name", out var jToken))
        {
            var val = jToken?.ToString();
            if (!string.IsNullOrEmpty(val)) return val;
        }
        return null;
    }

    private List<string> ExtractXMsAuthSchemasFromCustomProperties(JObject customProperties)
    {
        var result = new List<string>();
        if (customProperties != null && customProperties.TryGetValue("x-ms-auth-schemas", out var jToken))
        {
            if (jToken is JArray arr)
                foreach (var item in arr)
                    result.Add(item.ToString());
        }
        return result;
    }

    private List<string> ExtractXMsSecuritySchemesFromCustomProperties(JObject customProperties, List<string> xMsAuthSchemas)
    {
        var result = new List<string>();

        if (customProperties != null && customProperties.TryGetValue("x-ms-security-schemes", out var jToken))
        {
            if (jToken is JObject secSchemesObj)
            {
                foreach (var property in secSchemesObj.Properties())
                {
                    if (property.Value is not JObject propObj) continue;

                    var type = ExtractTypeFromSecuritySchemeProperties(propObj);
                    if (!string.IsNullOrEmpty(type))
                    {
                        if (type.Equals("oauth2", StringComparison.OrdinalIgnoreCase))
                        {
                            var authUrl = ExtractAuthUrlFromOauth2Properties(propObj);
                            if (!string.IsNullOrEmpty(authUrl) &&
                                WellKnownEntraAuthorizationDomains.Any(d => authUrl.StartsWith(d, StringComparison.OrdinalIgnoreCase)))
                            {
                                if (xMsAuthSchemas != null && xMsAuthSchemas.Any())
                                {
                                    xMsAuthSchemas.ForEach(scheme =>
                                    {
                                        if (!result.Contains(scheme)) result.Add(scheme);
                                    });
                                    continue;
                                }
                                result.Add("managedidentity");
                            }
                        }
                        result.Add(type);
                    }
                }
            }
        }

        if (result.Count == 0)
            result.Add("unauthenticated");

        return result.Distinct().ToList();
    }

    private string ExtractTypeFromSecuritySchemeProperties(JObject props)
    {
        if (props.TryGetValue("type", out var typeJToken))
        {
            var type = typeJToken?.ToString();
            if (!string.IsNullOrEmpty(type))
            {
                if (type.Equals("oauth2", StringComparison.OrdinalIgnoreCase) ||
                    type.Equals("apikey", StringComparison.OrdinalIgnoreCase))
                    return type.ToLowerInvariant();

                if (type.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                    props.TryGetValue("scheme", out var scheme) &&
                    scheme?.ToString().Equals("bearer", StringComparison.OrdinalIgnoreCase) == true)
                    return "apikey";
            }
        }
        return null;
    }

    private string ExtractAuthUrlFromOauth2Properties(JObject props)
    {
        if (props.TryGetValue("flows", out var flowsJToken) && flowsJToken is JObject flowsObj)
        {
            if (flowsObj.TryGetValue("authorizationCode", out var authCode) && authCode is JObject authCodeObj)
            {
                if (authCodeObj.TryGetValue("authorizationUrl", out var authUrl))
                    return authUrl?.ToString();
            }
        }
        return null;
    }

    private Dictionary<string, string> ExtractTagsFromCustomProperties(JObject customProperties)
    {
        var tags = new Dictionary<string, string>();
        if (customProperties != null && customProperties.TryGetValue("x-ms-tags", out var jToken))
        {
            if (jToken is JArray arr)
                foreach (var item in arr)
                    tags[item.ToString()] = item.ToString();
        }
        return tags;
    }

    private string ExtractApiCenterToolTypeFromCustomProperties(
        McpServerContractDetails mcp, string xMsConnectorName, string apiCenterName, Dictionary<string, string> tags)
    {
        if (tags != null && tags.ContainsKey("built-in"))
            return "built-in";

        if (!string.IsNullOrEmpty(xMsConnectorName) &&
            apiCenterName.Equals(PublicConnectorsRegistryApiCenterName, StringComparison.OrdinalIgnoreCase))
            return "connector";

        if (mcp.Remotes != null && mcp.Remotes.Any())
            return "remotes";

        if (mcp.CustomProperties != null && mcp.CustomProperties.TryGetValue("packages", out var pkg))
        {
            if (pkg is JArray arr && arr.Count > 0) return "local";
        }

        return "local";
    }

    private JArray ExtractContactsFromCustomProperties(ApiDetail apiDetail, JObject customProperties)
    {
        if (apiDetail.Contacts != null && apiDetail.Contacts.Count > 0)
            return apiDetail.Contacts;

        if (customProperties != null && customProperties.TryGetValue("x-ms-support", out var contactsJToken))
        {
            var contact = contactsJToken?.ToString();
            if (!string.IsNullOrWhiteSpace(contact))
                return new JArray(contact);
        }

        return apiDetail.Contacts;
    }

    private double SetPopularityForApiCenterTool(string apiCenterName, string entityId, string apiCenterToolType)
    {
        if (apiCenterToolType.Equals("built-in", StringComparison.OrdinalIgnoreCase))
            return BuiltInApiCenterToolsPopularity;
        return DefaultApiCenterToolsPopularity;
    }

    private static JObject ToJObject(object obj) =>
        obj == null ? null : JObject.FromObject(obj, JSerializer);

    private static JArray ToJArray(object obj) =>
        obj == null ? null : JArray.FromObject(obj, JSerializer);
}
