using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Index;

/// <summary>
/// Represents a request to drop the properties of an index.
/// </summary>
public sealed class DropIndexPropertiesReq
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
    /// The names of the properties to remove from the index.
    /// </summary>
    public IReadOnlyList<string> DeleteKeys { get; set; } = Array.Empty<string>();

    internal Grpc.AlterIndexRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrEmpty(DeleteKeys);

        var request = new Grpc.AlterIndexRequest
        {
            CollectionName = CollectionName,
            IndexName = string.IsNullOrEmpty(IndexName) ? Constants.DefaultIndexName : IndexName
        };
        request.DeleteKeys.AddRange(DeleteKeys);
        return request;
    }
}
