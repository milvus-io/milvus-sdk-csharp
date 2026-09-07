using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Index;

/// <summary>
/// Represents a request to alter the properties of an index.
/// </summary>
public sealed class AlterIndexPropertiesReq
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The index name. Defaults to <c>"_default_idx"</c>.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// The properties to set or update on the index.
    /// </summary>
    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

    /// <summary>
    /// The names of the properties to remove from the index.
    /// </summary>
    public IReadOnlyList<string>? DeleteKeys { get; set; }

    internal Grpc.AlterIndexRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        var request = new Grpc.AlterIndexRequest
        {
            CollectionName = CollectionName,
            IndexName = string.IsNullOrEmpty(IndexName) ? Constants.DefaultIndexName : IndexName
        };
        foreach (KeyValuePair<string, string> property in Properties)
        {
            request.ExtraParams.Add(new Grpc.KeyValuePair { Key = property.Key, Value = property.Value });
        }

        if (DeleteKeys is not null)
        {
            request.DeleteKeys.AddRange(DeleteKeys);
        }

        return request;
    }
}
