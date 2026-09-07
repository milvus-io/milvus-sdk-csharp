using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Index;

/// <summary>
/// Represents a request to create an index on a field.
/// </summary>
public sealed class CreateIndexReq
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The field name to create the index on.
    /// </summary>
    public string FieldName { get; set; } = "";

    /// <summary>
    /// The index type. Defaults to <see cref="IndexType.AutoIndex" />.
    /// </summary>
    public IndexType? IndexType { get; set; }

    /// <summary>
    /// The metric type. For vector fields, must match the metric used for search.
    /// </summary>
    public SimilarityMetricType? MetricType { get; set; }

    /// <summary>
    /// The index name. Defaults to <c>"_default_idx"</c>.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// Additional index parameters (e.g. <c>nlist</c>, <c>M</c>, <c>efConstruction</c>).
    /// </summary>
    public IDictionary<string, string> ExtraParams { get; } = new Dictionary<string, string>();

    internal Grpc.CreateIndexRequest ToGrpcCreateIndexRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(FieldName);

        var request = new Grpc.CreateIndexRequest
        {
            CollectionName = CollectionName,
            FieldName = FieldName,
            IndexName = string.IsNullOrEmpty(IndexName) ? Constants.DefaultIndexName : IndexName
        };

        if (IndexType is not null)
        {
            request.ExtraParams.Add(new Grpc.KeyValuePair
            {
                Key = Constants.IndexType,
                Value = IndexType.Value.ToWireString()
            });
        }

        if (MetricType is not null)
        {
            request.ExtraParams.Add(new Grpc.KeyValuePair
            {
                Key = Constants.MetricType,
                Value = MetricType.Value.ToWireString()
            });
        }

        foreach (KeyValuePair<string, string> parameter in ExtraParams)
        {
            request.ExtraParams.Add(new Grpc.KeyValuePair { Key = parameter.Key, Value = parameter.Value });
        }

        return request;
    }
}
