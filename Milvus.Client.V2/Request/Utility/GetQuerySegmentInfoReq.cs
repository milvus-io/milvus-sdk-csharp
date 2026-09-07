#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Utility;
public sealed class GetQuerySegmentInfoReq
{
    public string CollectionName { get; set; } = "";
    internal Grpc.GetQuerySegmentInfoRequest ToGrpcGetQuerySegmentInfoRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        return new Grpc.GetQuerySegmentInfoRequest { CollectionName = CollectionName };
    }
}
