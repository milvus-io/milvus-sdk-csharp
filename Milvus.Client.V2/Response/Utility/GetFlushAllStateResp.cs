namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The state of a flush-all operation.
/// </summary>
public sealed class GetFlushAllStateResp
{
    internal GetFlushAllStateResp(bool flushed) => Flushed = flushed;
    internal static GetFlushAllStateResp FromGrpc(Grpc.GetFlushAllStateResponse response) => new(response.Flushed);

    /// <summary>
    /// Whether the flush-all operation has completed.
    /// </summary>
    public bool Flushed { get; }
}
