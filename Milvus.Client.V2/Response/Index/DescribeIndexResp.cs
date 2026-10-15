using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Responses.Index;

/// <summary>
/// Represents the result of a <c>DescribeIndex</c> operation.
/// </summary>
public sealed class DescribeIndexResp
{
    internal DescribeIndexResp(IReadOnlyList<IndexDesc> indexes)
    {
        Indexes = indexes;
    }

    internal static DescribeIndexResp FromGrpc(Grpc.DescribeIndexResponse response)
        => new(response.IndexDescriptions.Select(IndexDesc.FromGrpc).ToList());

    /// <summary>
    /// The descriptions of the indexes on the field.
    /// </summary>
    public IReadOnlyList<IndexDesc> Indexes { get; }

    /// <summary>
    /// Finds the index description built on the given field, or <c>null</c> when the response has no index on
    /// that field. Mirrors the Java SDK's <c>getIndexDescByFieldName</c>.
    /// </summary>
    /// <param name="fieldName">The field name to find an index for.</param>
    public IndexDesc? GetIndexDescByFieldName(string fieldName)
    {
        Verify.NotNullOrWhiteSpace(fieldName, nameof(fieldName));
        return Indexes.FirstOrDefault(desc => desc.FieldName == fieldName);
    }

    /// <summary>
    /// Finds the index description with the given name, or <c>null</c> when the response has no index with that
    /// name. Mirrors the Java SDK's <c>getIndexDescByIndexName</c>.
    /// </summary>
    /// <param name="indexName">The index name to find.</param>
    public IndexDesc? GetIndexDescByIndexName(string indexName)
    {
        Verify.NotNullOrWhiteSpace(indexName, nameof(indexName));
        return Indexes.FirstOrDefault(desc => desc.IndexName == indexName);
    }
}

/// <summary>
/// Describes an index on a field of a collection, as reported by <c>DescribeIndex</c>/<c>ListIndexes</c>.
/// Carries the index build state and progress in addition to the create-time parameters (mirroring the Java
/// SDK's <c>DescribeIndexResp.IndexDesc</c>).
/// </summary>
public sealed class IndexDesc
{
    internal IndexDesc(
        string indexName, long indexId, string fieldName, IndexState state,
        long indexedRows, long totalRows, long pendingIndexRows, string? indexStateFailReason,
        IReadOnlyDictionary<string, string> parameters)
    {
        IndexName = indexName;
        IndexId = indexId;
        FieldName = fieldName;
        State = state;
        IndexedRows = indexedRows;
        TotalRows = totalRows;
        PendingIndexRows = pendingIndexRows;
        IndexStateFailReason = indexStateFailReason;
        Parameters = parameters;

        IndexType = parameters.TryGetValue("index_type", out string? indexType) && TryParseIndexType(indexType, out IndexType parsedIndexType)
            ? parsedIndexType
            : null;
        MetricType = parameters.TryGetValue("metric_type", out string? metricType) && TryParseMetricType(metricType, out SimilarityMetricType parsedMetricType)
            ? parsedMetricType
            : null;
    }

    internal static IndexDesc FromGrpc(Grpc.IndexDescription grpc)
    {
        var parameters = new Dictionary<string, string>();
        foreach (Grpc.KeyValuePair kv in grpc.Params)
        {
            parameters[kv.Key] = kv.Value;
        }

        return new IndexDesc(
            grpc.IndexName, grpc.IndexID, grpc.FieldName, (IndexState)grpc.State,
            grpc.IndexedRows, grpc.TotalRows, grpc.PendingIndexRows,
            string.IsNullOrEmpty(grpc.IndexStateFailReason) ? null : grpc.IndexStateFailReason,
            parameters);
    }

    /// <summary>
    /// The index name.
    /// </summary>
    public string IndexName { get; }

    /// <summary>
    /// The index id.
    /// </summary>
    public long IndexId { get; }

    /// <summary>
    /// The field name the index is built on.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// The index build state.
    /// </summary>
    public IndexState State { get; }

    /// <summary>
    /// The number of rows indexed so far.
    /// </summary>
    public long IndexedRows { get; }

    /// <summary>
    /// The total number of rows to index.
    /// </summary>
    public long TotalRows { get; }

    /// <summary>
    /// The number of rows pending index build.
    /// </summary>
    public long PendingIndexRows { get; }

    /// <summary>
    /// The failure reason when the index build failed, or <c>null</c>.
    /// </summary>
    public string? IndexStateFailReason { get; }

    /// <summary>
    /// The index parameters (e.g. <c>index_type</c>, <c>metric_type</c>, <c>nlist</c>).
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; }

    /// <summary>
    /// The typed index type, parsed from <see cref="Parameters" />'s <c>index_type</c> entry, or
    /// <c>null</c> when absent or unrecognized.
    /// </summary>
    public IndexType? IndexType { get; }

    /// <summary>
    /// The typed metric type, parsed from <see cref="Parameters" />'s <c>metric_type</c> entry, or
    /// <c>null</c> when absent or unrecognized (e.g. scalar indexes have no metric).
    /// </summary>
    public SimilarityMetricType? MetricType { get; }

    private static bool TryParseIndexType(string wire, out IndexType indexType)
    {
        foreach (IndexType value in Enum.GetValues(typeof(IndexType)))
        {
            if (value != Types.IndexType.Invalid && string.Equals(value.ToWireString(), wire, StringComparison.OrdinalIgnoreCase))
            {
                indexType = value;
                return true;
            }
        }

        indexType = Types.IndexType.Invalid;
        return false;
    }

    private static bool TryParseMetricType(string wire, out SimilarityMetricType metricType)
    {
        foreach (SimilarityMetricType value in Enum.GetValues(typeof(SimilarityMetricType)))
        {
            if (value != Types.SimilarityMetricType.Invalid && string.Equals(value.ToWireString(), wire, StringComparison.OrdinalIgnoreCase))
            {
                metricType = value;
                return true;
            }
        }

        metricType = Types.SimilarityMetricType.Invalid;
        return false;
    }
}
