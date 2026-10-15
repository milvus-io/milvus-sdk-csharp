using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Partition;

/// <summary>
/// Represents a request to create a partition in a collection.
/// </summary>
public sealed class CreatePartitionReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to create the partition in.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The name of the partition to create.
    /// </summary>
    public string PartitionName { get; set; } = "";
    internal Grpc.CreatePartitionRequest ToGrpcCreatePartitionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(PartitionName);
        var request = new Grpc.CreatePartitionRequest { CollectionName = CollectionName, PartitionName = PartitionName };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
