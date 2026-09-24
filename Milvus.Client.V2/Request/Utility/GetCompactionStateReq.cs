namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to get the state of a compaction.
/// </summary>
public sealed class GetCompactionStateReq
{
    /// <summary>
    /// The id of the compaction whose state to retrieve.
    /// </summary>
    public long CompactionId { get; set; }
    internal Grpc.GetCompactionStateRequest ToGrpcGetCompactionStateRequest()
        => new() { CompactionID = CompactionId };
}
