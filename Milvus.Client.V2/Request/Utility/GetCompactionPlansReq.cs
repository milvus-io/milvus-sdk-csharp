using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to get the compaction plans of a compaction.
/// </summary>
public sealed class GetCompactionPlansReq
{
    /// <summary>
    /// The id of the compaction whose plans to retrieve.
    /// </summary>
    public long CompactionId { get; set; }

    internal Grpc.GetCompactionPlansRequest ToGrpcGetCompactionPlansRequest()
    {
        Verify.GreaterThan(CompactionId, 0);
        return new Grpc.GetCompactionPlansRequest { CompactionID = CompactionId };
    }
}
