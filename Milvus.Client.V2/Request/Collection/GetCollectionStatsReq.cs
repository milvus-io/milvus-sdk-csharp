using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to get the statistics of a collection.
/// </summary>
public sealed class GetCollectionStatsReq
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.GetCollectionStatisticsRequest ToGrpcGetCollectionStatisticsRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        return new Grpc.GetCollectionStatisticsRequest { CollectionName = CollectionName };
    }
}
