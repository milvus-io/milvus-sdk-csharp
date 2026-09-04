namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The result of a flush-all operation.
/// </summary>
public sealed class FlushAllResp
{
    internal FlushAllResp(ulong flushAllTimestamp) => FlushAllTimestamp = flushAllTimestamp;
#pragma warning disable CS0612 // The server marks FlushAllTs as deprecated but still populates it.
    internal static FlushAllResp FromGrpc(Grpc.FlushAllResponse response) => new(response.FlushAllTs);
#pragma warning restore CS0612

    /// <summary>
    /// The timestamp identifying the flush-all operation, used to poll its state.
    /// </summary>
    public ulong FlushAllTimestamp { get; }
}
