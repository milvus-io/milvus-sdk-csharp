using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class DescribeIndexResponse
{
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    [JsonPropertyName("index_descriptions")]
    public IList<IndexDescription> IndexDescriptions { get; set; }

    public IEnumerable<MilvusIndex> ToMilvusIndexes()
    {
        if (IndexDescriptions?.Any() != true)
        {
            yield break;
        }

        foreach (var index in IndexDescriptions)
        {
            yield return new MilvusIndex(index.FieldName, index.IndexName, index.IndexId, index.Params);
        }
    }
}

internal sealed class IndexDescription
{
    [JsonPropertyName("field_name")]
    public string FieldName { get; set; }

    [JsonPropertyName("index_name")]
    public string IndexName { get; set; }

    [JsonPropertyName("index_name")]
    public long IndexId { get; set; }

    [JsonPropertyName("params")]
    [JsonConverter(typeof(MilvusDictionaryConverter))]
    public IDictionary<string,string> Params { get; set; }
}

/// <summary>
/// Milvus index.
/// </summary>
public class MilvusIndex
{
    /// <summary>
    /// Milvus Index.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="indexName">Index name.</param>
    /// <param name="indexId">Index id</param>
    /// <param name="params">Params</param>
    public MilvusIndex(
        string fieldName, 
        string indexName, 
        long indexId, 
        IDictionary<string, string> @params)
    {
        FieldName = fieldName;
        IndexName = indexName;
        IndexId = indexId;
        Params = @params;
    }

    /// <summary>
    /// Field name.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Index name.
    /// </summary>
    public string IndexName { get; }

    /// <summary>
    /// Index id.
    /// </summary>
    public long IndexId { get; }

    /// <summary>
    /// Contains index_type, metric_type, params.
    /// </summary>
    public IDictionary<string,string> Params { get; }
}
