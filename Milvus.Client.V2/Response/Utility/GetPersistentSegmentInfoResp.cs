#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Utility;
public sealed class GetPersistentSegmentInfoResp
{
    internal GetPersistentSegmentInfoResp(IReadOnlyList<PersistentSegmentInfo> infos) => Infos = infos;
    internal static GetPersistentSegmentInfoResp FromGrpc(Grpc.GetPersistentSegmentInfoResponse response)
        => new(response.Infos.Select(PersistentSegmentInfo.FromGrpc).ToList());
    public IReadOnlyList<PersistentSegmentInfo> Infos { get; }
}
