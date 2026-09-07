#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.ResourceGroup;
public sealed class TransferReplicaReq
{
    public string SourceResourceGroup { get; set; } = "";
    public string TargetResourceGroup { get; set; } = "";
    public string CollectionName { get; set; } = "";
    public long NumReplica { get; set; }
    internal Grpc.TransferReplicaRequest ToGrpcTransferReplicaRequest()
    {
        Verify.NotNullOrWhiteSpace(SourceResourceGroup);
        Verify.NotNullOrWhiteSpace(TargetResourceGroup);
        Verify.NotNullOrWhiteSpace(CollectionName);
        return new Grpc.TransferReplicaRequest
        {
            SourceResourceGroup = SourceResourceGroup,
            TargetResourceGroup = TargetResourceGroup,
            CollectionName = CollectionName,
            NumReplica = NumReplica
        };
    }
}
