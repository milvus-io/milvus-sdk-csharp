namespace Milvus.Client.V2.Responses.Collection;

/// <summary>
/// Represents the result of a <c>HasCollection</c> operation.
/// </summary>
public sealed class HasCollectionResp
{
    private HasCollectionResp(bool has)
    {
        Has = has;
    }

    internal static HasCollectionResp FromGrpc(Grpc.BoolResponse response)
        => new(response.Value);

    /// <summary>
    /// Whether the collection exists.
    /// </summary>
    public bool Has { get; }
}
