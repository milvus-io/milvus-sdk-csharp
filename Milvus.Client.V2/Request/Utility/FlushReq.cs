#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Utility;
public sealed class FlushReq
{
    public IReadOnlyList<string> CollectionNames { get; set; } = Array.Empty<string>();
    internal Grpc.FlushRequest ToGrpcFlushRequest()
    {
        Verify.NotNullOrEmpty(CollectionNames);
        var request = new Grpc.FlushRequest();
        request.CollectionNames.AddRange(CollectionNames);
        return request;
    }
}
