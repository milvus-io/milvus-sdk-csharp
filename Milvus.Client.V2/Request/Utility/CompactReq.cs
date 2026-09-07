#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Utility;
public sealed class CompactReq
{
    public string CollectionName { get; set; } = "";
    public bool IsMajorCompaction { get; set; }

    /// <summary>
    /// The target segment size in MB. When set, the server aims to produce segments of at most this size.
    /// </summary>
    public long? TargetSizeInMB { get; set; }

    internal Grpc.ManualCompactionRequest ToGrpcManualCompactionRequest(long collectionId)
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        var request = new Grpc.ManualCompactionRequest
        {
            CollectionID = collectionId,
            CollectionName = CollectionName,
            MajorCompaction = IsMajorCompaction
        };
        if (TargetSizeInMB is not null)
        {
            request.TargetSize = TargetSizeInMB.Value;
        }

        return request;
    }
}
