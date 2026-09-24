using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Partition;

/// <summary>
/// Represents a request to check whether a partition exists in a collection.
/// </summary>
public sealed class HasPartitionReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection that the partition belongs to.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The name of the partition to check.
    /// </summary>
    public string PartitionName { get; set; } = "";
    internal Grpc.HasPartitionRequest ToGrpcHasPartitionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(PartitionName);
        var request = new Grpc.HasPartitionRequest { CollectionName = CollectionName, PartitionName = PartitionName };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
