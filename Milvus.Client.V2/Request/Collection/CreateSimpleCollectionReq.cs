using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// A convenience request to create a simple collection with just a primary field and a float vector field,
/// mirroring the C++ SDK's <c>CreateSimpleCollectionRequest</c>. A vector index is created automatically
/// (<see cref="IndexType.AutoIndex" />) and the collection is loaded after creation.
/// </summary>
public sealed class CreateSimpleCollectionReq
{
    /// <summary>
    /// The name of the collection to create.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the primary-key field. Defaults to <c>"id"</c>.
    /// </summary>
    public string PrimaryFieldName { get; set; } = "id";

    /// <summary>
    /// The data type of the primary-key field: <see cref="DataType.Int64" /> (default) or
    /// <see cref="DataType.VarChar" />.
    /// </summary>
    public DataType PrimaryFieldType { get; set; } = DataType.Int64;

    /// <summary>
    /// The maximum length of the primary-key field when <see cref="PrimaryFieldType" /> is
    /// <see cref="DataType.VarChar" />. Defaults to 65535.
    /// </summary>
    public int MaxLength { get; set; } = 65535;

    /// <summary>
    /// The name of the vector field. Defaults to <c>"vector"</c>.
    /// </summary>
    public string VectorFieldName { get; set; } = "vector";

    /// <summary>
    /// The dimensionality of the vector field. Must be specified (non-zero).
    /// </summary>
    public int Dimension { get; set; }

    /// <summary>
    /// The consistency level of the collection. Defaults to <see cref="ConsistencyLevel.BoundedStaleness" />.
    /// </summary>
    public ConsistencyLevel ConsistencyLevel { get; set; } = ConsistencyLevel.BoundedStaleness;

    /// <summary>
    /// The metric type used for the vector index and search. Defaults to <see cref="SimilarityMetricType.Cosine" />.
    /// </summary>
    public SimilarityMetricType MetricType { get; set; } = SimilarityMetricType.Cosine;

    /// <summary>
    /// Whether primary-key values are automatically generated. Defaults to <c>false</c>.
    /// </summary>
    public bool AutoID { get; set; }

    /// <summary>
    /// Whether to enable dynamic fields. Defaults to <c>true</c>, matching the C++ SDK.
    /// </summary>
    public bool EnableDynamicFields { get; set; } = true;
}
