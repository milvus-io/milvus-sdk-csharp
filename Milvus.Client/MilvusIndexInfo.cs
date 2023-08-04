namespace Milvus.Client;

/// <summary>
/// Contains information about an index in Milvus; returned from <see cref="MilvusCollection.DescribeIndexAsync" />.
/// </summary>
public sealed class MilvusIndexInfo
{
    internal MilvusIndexInfo(
        string fieldName,
        string indexName,
        long indexId,
        IReadOnlyDictionary<string, string> @params)
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
    public IReadOnlyDictionary<string, string> Params { get; }

    /// <summary>
    /// Get string data of <see cref="MilvusIndexInfo"/>
    /// </summary>
    /// <returns></returns>
    public override string ToString()
        => $"MilvusIndex: {{{nameof(FieldName)}: {FieldName}, {nameof(IndexName)}: {IndexName}, {nameof(IndexId)}: {IndexId}}}";
}
