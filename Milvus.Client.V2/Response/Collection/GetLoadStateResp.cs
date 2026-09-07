using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Responses.Collection;

/// <summary>
/// Represents the result of a <c>GetLoadState</c> operation.
/// </summary>
public sealed class GetLoadStateResp
{
    private GetLoadStateResp(LoadState state)
    {
        State = state;
    }

    internal static GetLoadStateResp FromGrpc(Grpc.GetLoadStateResponse response)
        => new((LoadState)response.State);

    /// <summary>
    /// The load state of the collection.
    /// </summary>
    public LoadState State { get; }
}
