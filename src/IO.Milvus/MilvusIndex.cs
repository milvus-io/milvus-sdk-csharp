using System.Collections.Generic;

namespace IO.Milvus;

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
    public IDictionary<string, string> Params { get; }
}
