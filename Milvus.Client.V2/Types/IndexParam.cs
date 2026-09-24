using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// Describes an index to create as part of <c>CreateCollectionAsync</c>, mirroring the Java SDK's <c>IndexParam</c> /
/// Java <c>IndexParam</c>. The index is created immediately after the collection is created, and the collection
/// is then loaded automatically.
/// </summary>
public sealed class IndexParam
{
    /// <summary>
    /// Creates an index descriptor.
    /// </summary>
    public IndexParam(string fieldName, string? indexName = null, IndexType? indexType = null,
        SimilarityMetricType? metricType = null)
    {
        Verify.NotNullOrWhiteSpace(fieldName);
        FieldName = fieldName;
        IndexName = indexName;
        IndexType = indexType;
        MetricType = metricType;
    }

    /// <summary>
    /// The field name to create the index on.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// The index name. Defaults to <c>"_default_idx"</c>.
    /// </summary>
    public string? IndexName { get; }

    /// <summary>
    /// The index type. When unset, the index type is omitted from the request and the server picks its default.
    /// </summary>
    public IndexType? IndexType { get; }

    /// <summary>
    /// The metric type. For vector fields, must match the metric used for search.
    /// </summary>
    public SimilarityMetricType? MetricType { get; }

    /// <summary>
    /// Additional index parameters (e.g. <c>nlist</c>, <c>M</c>, <c>efConstruction</c>). Values are serialized
    /// to their string form on the wire (mirroring the Java SDK's <c>Map&lt;String,Object&gt; extraParams</c>,
    /// which calls <c>String.valueOf</c>). The <c>dim</c> entry must be an integral value.
    /// </summary>
    public IDictionary<string, object> ExtraParams { get; } = new Dictionary<string, object>();
}
