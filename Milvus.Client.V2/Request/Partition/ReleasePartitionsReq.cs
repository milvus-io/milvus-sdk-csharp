using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Partition;

/// <summary>
/// Represents a request to release partitions of a collection from memory.
/// </summary>
public sealed class ReleasePartitionsReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection that contains the partitions to release.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The names of the partitions to release.
    /// </summary>
    public IReadOnlyList<string> PartitionNames { get; set; } = Array.Empty<string>();
    internal Grpc.ReleasePartitionsRequest ToGrpcReleasePartitionsRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrEmpty(PartitionNames);
        var request = new Grpc.ReleasePartitionsRequest { CollectionName = CollectionName };
        request.PartitionNames.AddRange(PartitionNames);
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
