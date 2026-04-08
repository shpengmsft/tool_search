using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

namespace ApiCenterIndexUploader;

/// <summary>
/// Creates and manages the AI Search index with the exact same schema as Vienna's azureml-v0-9.
/// Replicates: Index/Services.AzureSearch/SearchSchemaProvider.cs + CustomAnalyzersProvider.cs
/// </summary>
public class IndexManager
{
    private readonly SearchIndexClient _indexClient;
    private readonly string _indexName;

    public IndexManager(string endpoint, string indexName, string adminKey = null)
    {
        if (!string.IsNullOrEmpty(adminKey))
            _indexClient = new SearchIndexClient(new Uri(endpoint), new AzureKeyCredential(adminKey));
        else
            _indexClient = new SearchIndexClient(new Uri(endpoint), new Azure.Identity.DefaultAzureCredential());
        _indexName = indexName;
        _adminKey = adminKey;
    }

    private readonly string _adminKey;

    /// <summary>
    /// Creates the index with Vienna's exact schema including custom analyzers.
    /// </summary>
    public async Task CreateOrUpdateIndexAsync()
    {
        // Build index from SearchDocument class attributes
        var fieldBuilder = new FieldBuilder();
        var fields = fieldBuilder.Build(typeof(SearchDocument));

        var index = new SearchIndex(_indexName)
        {
            Fields = fields,
        };

        // ── Custom analyzers (same as Vienna CustomAnalyzersProvider) ──
        // Edge NGram token filter for prefix matching
        var ngramTokenFilter = new EdgeNGramTokenFilter("prefix-ngram-token-filter")
        {
            MinGram = 2,
            MaxGram = 50,
            Side = EdgeNGramTokenFilterSide.Front
        };
        index.TokenFilters.Add(ngramTokenFilter);

        // Index analyzer: standard tokenizer + lowercase + kstem + stopwords + edge ngram
        var edgeNgramAnalyzer = new CustomAnalyzer("prefix-ngram-analyzer", LexicalTokenizerName.Standard);
        edgeNgramAnalyzer.TokenFilters.Add(TokenFilterName.Lowercase);
        edgeNgramAnalyzer.TokenFilters.Add(TokenFilterName.KStem);
        edgeNgramAnalyzer.TokenFilters.Add(TokenFilterName.Stopwords);
        edgeNgramAnalyzer.TokenFilters.Add("prefix-ngram-token-filter");
        index.Analyzers.Add(edgeNgramAnalyzer);

        // Search analyzer: standard tokenizer + lowercase + kstem + stopwords (no ngram)
        var searchAnalyzer = new CustomAnalyzer("prefix-search-analyzer", LexicalTokenizerName.Standard);
        searchAnalyzer.TokenFilters.Add(TokenFilterName.Lowercase);
        searchAnalyzer.TokenFilters.Add(TokenFilterName.KStem);
        searchAnalyzer.TokenFilters.Add(TokenFilterName.Stopwords);
        index.Analyzers.Add(searchAnalyzer);

        // ── Apply custom analyzers to AllStringFieldValuesSerialized field ──
        // FieldBuilder doesn't support custom analyzer attributes, so we patch the field manually
        var allStringsField = fields.FirstOrDefault(f => f.Name == "AllStringFieldValuesSerialized")
                              ?? fields.FirstOrDefault(f => f.Name == "allStringFieldValuesSerialized");
        if (allStringsField != null)
        {
            // Remove the field and re-add with custom analyzers
            fields.Remove(allStringsField);
            var customField = new SearchableField("AllStringFieldValuesSerialized", false)
            {
                IndexAnalyzerName = "prefix-ngram-analyzer",
                SearchAnalyzerName = "prefix-search-analyzer",
                IsHidden = false
            };
            fields.Add(customField);
        }

        // ── Semantic configuration (exists in Vienna but disabled) ──
        var semanticConfig = new SemanticConfiguration("tools-semantic-config",
            new SemanticPrioritizedFields
            {
                TitleField = new SemanticField("Name"),
                ContentFields = {
                    new SemanticField("AnnotationsSerialized"),
                    new SemanticField("AllStringFieldValuesSerialized")
                },
                KeywordsFields = {
                    new SemanticField("Labels")
                }
            });

        index.SemanticSearch = new SemanticSearch
        {
            Configurations = { semanticConfig }
        };

        await _indexClient.CreateOrUpdateIndexAsync(index);
    }

    public SearchClient GetSearchClient()
    {
        if (!string.IsNullOrEmpty(_adminKey))
            return new SearchClient(new Uri(_indexClient.Endpoint.ToString()), _indexName, new AzureKeyCredential(_adminKey));
        return new SearchClient(
            new Uri(_indexClient.Endpoint.ToString()),
            _indexName,
            new Azure.Identity.DefaultAzureCredential());
    }

    /// <summary>
    /// Uploads documents in batches (same as Vienna's batch upload pattern).
    /// </summary>
    public async Task<int> UploadDocumentsAsync(List<SearchDocument> documents, int batchSize = 1000)
    {
        var searchClient = GetSearchClient();
        int uploaded = 0;

        for (int i = 0; i < documents.Count; i += batchSize)
        {
            var batch = documents.Skip(i).Take(batchSize).ToList();
            var actions = batch.Select(d => IndexDocumentsAction.MergeOrUpload(d)).ToList();
            var indexBatch = IndexDocumentsBatch.Create(actions.ToArray());

            var result = await searchClient.IndexDocumentsAsync(indexBatch);
            var succeeded = result.Value.Results.Count(r => r.Succeeded);
            var failed = result.Value.Results.Count(r => !r.Succeeded);
            uploaded += succeeded;

            Console.WriteLine($"  Batch {i / batchSize + 1}: {succeeded} succeeded, {failed} failed");

            if (failed > 0)
            {
                foreach (var r in result.Value.Results.Where(r => !r.Succeeded))
                    Console.WriteLine($"    FAILED: {r.Key} — {r.ErrorMessage}");
            }
        }

        return uploaded;
    }
}
