using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Responses.Collection;

/// <summary>
/// Represents the result of a <c>GetLoadState</c> operation.
/// </summary>
public sealed class GetLoadStateResp
{
    internal GetLoadStateResp(LoadState state, long progress)
    {
        State = state;
        Progress = progress;
    }

    internal static GetLoadStateResp FromGrpc(Grpc.GetLoadStateResponse response)
        => new((LoadState)response.State, 0);

    /// <summary>
    /// The load state of the collection.
    /// </summary>
    public LoadState State { get; }

    /// <summary>
    /// The loading progress of the collection as a percentage (0–100), populated when the state is
    /// <see cref="LoadState.Loading" /> (via the <c>GetLoadingProgress</c> RPC) or
    /// <see cref="LoadState.Loaded" /> (100). Otherwise 0.
    /// </summary>
    public long Progress { get; internal set; }
}
