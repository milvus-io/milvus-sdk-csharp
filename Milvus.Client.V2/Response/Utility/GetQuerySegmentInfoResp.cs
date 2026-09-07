#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Utility;
public sealed class GetQuerySegmentInfoResp
{
    internal GetQuerySegmentInfoResp(IReadOnlyList<QuerySegmentInfo> infos) => Infos = infos;
    internal static GetQuerySegmentInfoResp FromGrpc(Grpc.GetQuerySegmentInfoResponse response)
        => new(response.Infos.Select(QuerySegmentInfo.FromGrpc).ToList());
    public IReadOnlyList<QuerySegmentInfo> Infos { get; }
}
