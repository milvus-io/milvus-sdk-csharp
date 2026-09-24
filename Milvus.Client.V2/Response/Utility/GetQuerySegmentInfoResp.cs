namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The query-segment info of a loaded collection.
/// </summary>
public sealed class GetQuerySegmentInfoResp
{
    internal GetQuerySegmentInfoResp(IReadOnlyList<QuerySegmentInfo> infos) => Infos = infos;
    internal static GetQuerySegmentInfoResp FromGrpc(Grpc.GetQuerySegmentInfoResponse response, string collectionName = "")
        => new(response.Infos.Select(i => QuerySegmentInfo.FromGrpc(i, collectionName)).ToList());

    /// <summary>
    /// The loaded query segments of the collection.
    /// </summary>
    public IReadOnlyList<QuerySegmentInfo> Infos { get; }
}
