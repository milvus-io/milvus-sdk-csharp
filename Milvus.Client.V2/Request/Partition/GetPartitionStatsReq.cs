using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Partition;

/// <summary>
/// Represents a request to get statistics about a partition.
/// </summary>
public sealed class GetPartitionStatsReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection that contains the partition.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The name of the partition to get statistics for.
    /// </summary>
    public string PartitionName { get; set; } = "";
    internal Grpc.GetPartitionStatisticsRequest ToGrpcGetPartitionStatisticsRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(PartitionName);
        var request = new Grpc.GetPartitionStatisticsRequest { CollectionName = CollectionName, PartitionName = PartitionName };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
