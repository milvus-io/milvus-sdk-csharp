#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.ResourceGroup;
public sealed class TransferNodeReq
{
    public string SourceResourceGroup { get; set; } = "";
    public string TargetResourceGroup { get; set; } = "";
    public int NumNode { get; set; }
    internal Grpc.TransferNodeRequest ToGrpcTransferNodeRequest()
    {
        Verify.NotNullOrWhiteSpace(SourceResourceGroup);
        Verify.NotNullOrWhiteSpace(TargetResourceGroup);
        return new Grpc.TransferNodeRequest
        {
            SourceResourceGroup = SourceResourceGroup,
            TargetResourceGroup = TargetResourceGroup,
            NumNode = NumNode
        };
    }
}
