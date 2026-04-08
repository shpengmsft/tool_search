using ApiCenterMcpFetcher;
using ApiCenterMcpFetcher.Contracts;
using Newtonsoft.Json;

// ── Public API Centers (same as Vienna AgentAssetServiceConfiguration defaults) ──
var publicApiCenters = new[]
{
    new {
        Name = "registry-prod-bl",
        Location = "eastus",
        ArmId = "/subscriptions/d105e4a9-bf86-4040-b97c-564af427727f/resourceGroups/public-registry-eastus/providers/Microsoft.ApiCenter/services/registry-prod-bl"
    },
    new {
        Name = "connectors-registry-prod-bl",
        Location = "eastus",
        ArmId = "/subscriptions/d105e4a9-bf86-4040-b97c-564af427727f/resourceGroups/public-registry-eastus/providers/Microsoft.ApiCenter/services/connectors-registry-prod-bl"
    }
};

var client = new ApiCenterClient();
var utilities = new ApiCenterIndexServiceUtilities();
var allEntities = new List<ApiCenterToolVersionedIndexEntity>();

foreach (var apiCenter in publicApiCenters)
{
    Console.WriteLine($"\n══ API Center: {apiCenter.Name} ══");

    // Step 1: List all APIs (same as GetAllApis)
    Console.Write("  Fetching APIs...");
    var apis = await client.GetAllApis(apiCenter.Name, apiCenter.Location);
    Console.WriteLine($" {apis.Count} found");

    // Step 2: Get MCP servers for each API (same as GetMcpServersForApis)
    Console.Write("  Fetching MCP server details...");
    var mcpServers = await client.GetMcpServersForApis(apiCenter.Name, apiCenter.Location, apis);
    Console.WriteLine($" {mcpServers.Count} matched");

    // Step 3: Transform → ApiCenterToolVersionedIndexEntity (same as ConstructApiCenterToolVersionedIndexEntity)
    int count = 0;
    foreach (var api in apis)
    {
        if (!mcpServers.ContainsKey(api.Name)) continue;

        var mcpServer = mcpServers[api.Name];
        var entity = utilities.ConstructApiCenterToolVersionedIndexEntity(
            resourceLocation: apiCenter.Location,
            apiCenterName: apiCenter.Name,
            mcpServerContractDetails: mcpServer,
            version: "1",
            apiDetail: api,
            apiCenterArmScope: apiCenter.ArmId);

        allEntities.Add(entity);
        count++;
    }
    Console.WriteLine($"  Transformed {count} entities");
}

// Save to JSON
var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "ApiCenterToolEntities.json");
var json = JsonConvert.SerializeObject(allEntities, Formatting.Indented, new JsonSerializerSettings
{
    NullValueHandling = NullValueHandling.Ignore
});
File.WriteAllText(outputPath, json);

Console.WriteLine($"\n✓ Saved {allEntities.Count} ApiCenterToolVersionedIndexEntity objects to:");
Console.WriteLine($"  {outputPath}");
Console.WriteLine($"  File size: {new FileInfo(outputPath).Length / 1024.0:F1} KB");
