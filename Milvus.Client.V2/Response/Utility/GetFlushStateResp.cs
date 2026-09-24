namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The result of a <c>GetFlushState</c> check.
/// </summary>
public sealed class GetFlushStateResp
{
    internal GetFlushStateResp(bool flushed) => Flushed = flushed;

    internal static GetFlushStateResp FromGrpc(Grpc.GetFlushStateResponse response)
        => new(response.Flushed);

    /// <summary>
    /// Whether all the checked segments have been flushed.
    /// </summary>
    public bool Flushed { get; }
}
