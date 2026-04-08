using System;
using System.Collections.Generic;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace ApiCenterIndexUploader;

/// <summary>
/// Replicates Vienna's SearchDocument_2020_02_02 schema exactly.
/// Source: Index/Services.AzureSearch/Schemas/SearchDocument_2020_02_02.cs
/// </summary>
public class SearchDocument
{
    [SimpleField(IsKey = true)]
    public string Id { get; set; }

    [SearchableField(IsFilterable = true, IsSortable = true)]
    public string Type { get; set; }

    [SearchableField(IsFilterable = true, IsSortable = true)]
    public string Kind { get; set; }

    [SearchableField(IsFilterable = true)]
    public string SchemaId { get; set; }

    [SearchableField(IsFilterable = true, IsSortable = true, NormalizerName = LexicalNormalizerName.Values.Standard)]
    public string Name { get; set; }

    [SimpleField(IsFilterable = true, NormalizerName = LexicalNormalizerName.Values.Standard)]
    public string ResourceTenantId { get; set; }

    [SimpleField(IsFilterable = true, NormalizerName = LexicalNormalizerName.Values.Standard)]
    public string ShardingTenantId { get; set; }

    [SimpleField(IsFilterable = true)]
    public int ResourceShardingNumber { get; set; }

    [SearchableField(IsFilterable = true)]
    public string EntityContainerId { get; set; }

    [SearchableField(IsFilterable = true)]
    public string EntityContainerIdToLower { get; set; }

    [SearchableField(IsFilterable = true, IsSortable = true, AnalyzerName = LexicalAnalyzerName.Values.Keyword, NormalizerName = LexicalNormalizerName.Values.Standard)]
    public string Version { get; set; }

    [SearchableField(IsFilterable = true, IsSortable = true)]
    public string EntityObjectId { get; set; }

    [SimpleField(IsFilterable = true)]
    public string ResourceType { get; set; }

    [SearchableField(IsFilterable = true, IsSortable = true, NormalizerName = LexicalNormalizerName.Values.Standard)]
    public string EntityResourceName { get; set; }

    public long UpdateSequence { get; set; }

    [SearchableField(IsFilterable = true)]
    public string EntityId { get; set; }

    [SearchableField(IsFilterable = true)]
    public string AssetId { get; set; }

    [SimpleField(IsFilterable = true, IsSortable = true)]
    public UsageInternal Usage { get; set; }

    [SearchableField(IsFilterable = true, NormalizerName = LexicalNormalizerName.Values.Standard)]
    public List<string> Labels { get; set; }

    [SimpleField(IsFilterable = true, IsSortable = true)]
    public List<RelationshipImpl> Relationships { get; set; }

    [SearchableField(IsFilterable = true, IsSortable = true)]
    public string Repository { get; set; }

    [SimpleField(IsFilterable = true, IsSortable = true)]
    public long? InvisibleUntil { get; set; }

    [SimpleField(IsFilterable = true, IsSortable = true)]
    public long? CreatedTime { get; set; }

    [SimpleField(IsFilterable = true, IsSortable = true)]
    public long? UpdatedTime { get; set; }

    [SimpleField(IsFilterable = true)]
    public string CreatedBy { get; set; }

    [SimpleField(IsFilterable = true)]
    public long LastRebuildUnixTimestampInMs { get; set; }

    [SearchableField(IsHidden = false)]
    public string AnnotationsSerialized { get; set; }

    [SearchableField(IsFilterable = true)]
    public List<NameValuePairString> StringAnnotations { get; set; }

    [SearchableField(IsFilterable = true)]
    public List<NameValuePairString> StringProperties { get; set; }

    [SearchableField(IsFilterable = true)]
    public List<NameValuePairDouble> DoubleAnnotations { get; set; }

    [SearchableField(IsFilterable = true)]
    public List<NameValuePairDouble> DoubleProperties { get; set; }

    [SearchableField(IsFilterable = true)]
    public List<NameValuePairDateTimeOffset> DateTimeOffsetAnnotations { get; set; }

    [SearchableField(IsFilterable = true)]
    public List<NameValuePairDateTimeOffset> DateTimeOffsetProperties { get; set; }

    [SearchableField(IsFilterable = true)]
    public List<NameKeywordPair> StringTags { get; set; }

    [SearchableField(IsFilterable = true)]
    public List<NameTagPair> SearchableTags { get; set; }

    [SearchableField(IsHidden = false)]
    public string PropertiesSerialized { get; set; }

    [SearchableField(IsHidden = false)]
    public string AllStringFieldValuesSerialized { get; set; }
}

public class NameValuePairString
{
    [SearchableField(IsFilterable = true)]
    public string Name { get; set; }

    [SimpleField(IsFilterable = true)]
    public string Value { get; set; }
}

public class NameValuePairDouble
{
    [SearchableField(IsFilterable = true)]
    public string Name { get; set; }

    [SimpleField(IsFilterable = true)]
    public double Value { get; set; }
}

public class NameValuePairDateTimeOffset
{
    [SearchableField(IsFilterable = true)]
    public string Name { get; set; }

    [SimpleField(IsFilterable = true)]
    public DateTimeOffset Value { get; set; }
}

public class NameKeywordPair
{
    [SearchableField(IsFilterable = true, AnalyzerName = LexicalAnalyzerName.Values.Keyword)]
    public string Name { get; set; }

    [SearchableField(IsFilterable = true, AnalyzerName = LexicalAnalyzerName.Values.Keyword)]
    public string Value { get; set; }
}

public class NameTagPair
{
    [SearchableField(IsFilterable = true)]
    public string Name { get; set; }

    [SearchableField(IsFilterable = true)]
    public string Value { get; set; }
}

public class RelationshipImpl
{
    [SearchableField(IsFilterable = true, NormalizerName = LexicalNormalizerName.Values.Standard)]
    public string TargetEntityId { get; set; }

    [SearchableField(IsFilterable = true, NormalizerName = LexicalNormalizerName.Values.Standard)]
    public string AssetId { get; set; }

    [SearchableField(IsFilterable = true, NormalizerName = LexicalNormalizerName.Values.Standard)]
    public string RelationType { get; set; }

    [SearchableField(IsFilterable = true, NormalizerName = LexicalNormalizerName.Values.Standard)]
    public string EntityContainerId { get; set; }

    [SearchableField(IsFilterable = true, NormalizerName = LexicalNormalizerName.Values.Standard)]
    public string EntityType { get; set; }

    [SearchableField(IsFilterable = true, NormalizerName = LexicalNormalizerName.Values.Standard)]
    public string Direction { get; set; }
}

public class UsageInternal
{
    [SimpleField(IsFilterable = true, IsSortable = true)]
    public long? TotalCount { get; set; }

    [SimpleField(IsFilterable = true, IsSortable = true)]
    public double? Popularity { get; set; }
}
