namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The persistent segment info of a collection.
/// </summary>
public sealed class GetPersistentSegmentInfoResp
{
    internal GetPersistentSegmentInfoResp(IReadOnlyList<PersistentSegmentInfo> infos) => Infos = infos;
    internal static GetPersistentSegmentInfoResp FromGrpc(Grpc.GetPersistentSegmentInfoResponse response, string collectionName = "")
        => new(response.Infos.Select(i => PersistentSegmentInfo.FromGrpc(i, collectionName)).ToList());

    /// <summary>
    /// The persistent segments of the collection.
    /// </summary>
    public IReadOnlyList<PersistentSegmentInfo> Infos { get; }
}
