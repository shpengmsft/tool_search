using ApiCenterIndexUploader;
using Newtonsoft.Json;

// ── Configuration ──
var searchEndpoint = "https://shpeng-uksouth-acs.search.windows.net";
var indexName = "azureml-tools-test-v0-9";
var adminKey = Environment.GetEnvironmentVariable("SEARCH_ADMIN_KEY");
var inputFile = @".\ApiCenterToolEntities.json";

if (!File.Exists(inputFile))
{
    Console.WriteLine($"ERROR: Input file not found: {inputFile}");
    Console.WriteLine("Usage: dotnet run [path-to-ApiCenterToolEntities.json]");
    return 1;
}

Console.WriteLine($"── ApiCenter Index Uploader ──");
Console.WriteLine($"  Search endpoint: {searchEndpoint}");
Console.WriteLine($"  Index name:      {indexName}");
Console.WriteLine($"  Input file:      {inputFile}");

// ── Step 1: Read input entities ──
Console.WriteLine($"\n[1/3] Reading entities from {inputFile}...");
var json = File.ReadAllText(inputFile);
var entities = JsonConvert.DeserializeObject<List<ApiCenterToolEntity>>(json);
Console.WriteLine($"  Loaded {entities.Count} entities");

// ── Step 2: Create/Update index with Vienna schema ──
Console.WriteLine($"\n[2/3] Creating index '{indexName}' with Vienna schema...");
var indexManager = new IndexManager(searchEndpoint, indexName, adminKey);
await indexManager.CreateOrUpdateIndexAsync();
Console.WriteLine($"  Index created/updated successfully");

// ── Step 3: Convert entities → search documents and upload ──
Console.WriteLine($"\n[3/3] Converting and uploading documents...");
var documents = new List<SearchDocument>();
foreach (var entity in entities)
{
    var doc = EntityToDocumentConverter.ConvertToDocument(entity);
    documents.Add(doc);
}
Console.WriteLine($"  Converted {documents.Count} documents");

Console.WriteLine($"  Uploading in batches...");
var uploaded = await indexManager.UploadDocumentsAsync(documents);

Console.WriteLine($"\n✓ Done! {uploaded}/{documents.Count} documents uploaded to index '{indexName}'");
Console.WriteLine($"  View in portal: https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/921496dc-987f-410f-bd57-426eb2611356/resourceGroups/shpeng-uksouth-rg/providers/Microsoft.Search/searchServices/shpeng-uksouth-acs/indexes");
return 0;
