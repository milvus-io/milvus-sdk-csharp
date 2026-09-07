#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Utility;
public sealed class GetPersistentSegmentInfoReq
{
    public string CollectionName { get; set; } = "";
    internal Grpc.GetPersistentSegmentInfoRequest ToGrpcGetPersistentSegmentInfoRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        return new Grpc.GetPersistentSegmentInfoRequest { CollectionName = CollectionName };
    }
}
