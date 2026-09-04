using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Partition;

/// <summary>
/// Represents a request to drop a partition from a collection.
/// </summary>
public sealed class DropPartitionReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection that contains the partition to drop.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The name of the partition to drop.
    /// </summary>
    public string PartitionName { get; set; } = "";
    internal Grpc.DropPartitionRequest ToGrpcDropPartitionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(PartitionName);
        var request = new Grpc.DropPartitionRequest { CollectionName = CollectionName, PartitionName = PartitionName };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
