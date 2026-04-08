using ApiCenterMcpFetcher;
using ApiCenterMcpFetcher.Contracts;
using Newtonsoft.Json;

// ── Public API Centers (same as Vienna AgentAssetServiceConfiguration defaults) ──
var publicApiCenters = new[]
{
    new { Name = "registry-prod-bl", Location = "eastus" },
//    new { Name = "connectors-registry-prod-bl", Location = "eastus" },
};

var client = new ApiCenterClient();

foreach (var apiCenter in publicApiCenters)
{
    Console.WriteLine($"\n{"",0}{"═",0}{new string('═', 70)}");
    Console.WriteLine($"  API Center: {apiCenter.Name}");
    Console.WriteLine($"  Base URL:   https://{apiCenter.Name}.data.{apiCenter.Location}.azure-apicenter.ms");
    Console.WriteLine($"{new string('═', 72)}");

    // ── Step 1: List all APIs (same as GetAllApis) ──
    Console.WriteLine("\n── Step 1: Fetching all APIs ──");
    var apis = await client.GetAllApis(apiCenter.Name, apiCenter.Location);
    Console.WriteLine($"  Total APIs found: {apis.Count}");

    // ── Step 2: List all MCP servers (same as GetAllMcpServers) ──
    Console.WriteLine("\n── Step 2: Fetching all MCP servers ──");
    var mcpServers = await client.GetAllMcpServers(apiCenter.Name, apiCenter.Location);
    Console.WriteLine($"  Total MCP servers found: {mcpServers.Count}");

    // ── Step 3: Match APIs with MCP servers (same as GetMcpServersForApis) ──
    Console.WriteLine("\n── Step 3: Matching APIs to MCP servers ──");
    var mcpServersByName = await client.GetMcpServersForApis(apiCenter.Name, apiCenter.Location, apis);
    Console.WriteLine($"  APIs with MCP server entries: {mcpServersByName.Count} / {apis.Count}");

    // ── Print results ──
    Console.WriteLine($"\n── Results ──");
    int count = 0;
    foreach (var api in apis)
    {
        count++;
        var hasMcp = mcpServersByName.ContainsKey(api.Name);
        var mcpServer = hasMcp ? mcpServersByName[api.Name] : null;

        Console.WriteLine($"\n  [{count}] {api.Name}");
        Console.WriteLine($"      Title:       {api.Title ?? "(none)"}");
        Console.WriteLine($"      Kind:        {api.Kind ?? "(none)"}");
        Console.WriteLine($"      Description: {Truncate(api.Description, 100)}");
        Console.WriteLine($"      Has MCP:     {hasMcp}");

        if (mcpServer != null)
        {
            Console.WriteLine($"      MCP Name:    {mcpServer.Name}");
            Console.WriteLine($"      MCP Desc:    {Truncate(mcpServer.Description, 100)}");
            if (mcpServer.Repository != null)
                Console.WriteLine($"      Repo:        {mcpServer.Repository.Url ?? "(none)"}");
            if (mcpServer.VersionDetail != null)
                Console.WriteLine($"      Version:     {mcpServer.VersionDetail.Version} (Latest: {mcpServer.VersionDetail.IsLatest})");
            if (mcpServer.Remotes != null && mcpServer.Remotes.Length > 0)
            {
                foreach (var remote in mcpServer.Remotes)
                    Console.WriteLine($"      Remote:      [{remote.TransportType}] {remote.Url}");
            }
        }
    }

    Console.WriteLine($"\n  Summary: {apis.Count} APIs, {mcpServersByName.Count} with MCP server data");
}

Console.WriteLine("\n\nDone!");

static string Truncate(string s, int maxLen) =>
    string.IsNullOrEmpty(s) ? "(none)" :
    s.Length <= maxLen ? s : s[..maxLen] + "...";
