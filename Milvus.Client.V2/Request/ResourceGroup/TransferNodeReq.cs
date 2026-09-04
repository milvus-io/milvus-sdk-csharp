using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.ResourceGroup;

/// <summary>
/// Represents a request to transfer query nodes from one resource group to another.
/// </summary>
public sealed class TransferNodeReq
{
    /// <summary>
    /// The name of the resource group to transfer nodes from.
    /// </summary>
    public string SourceResourceGroup { get; set; } = "";

    /// <summary>
    /// The name of the resource group to transfer nodes to.
    /// </summary>
    public string TargetResourceGroup { get; set; } = "";

    /// <summary>
    /// The number of query nodes to transfer.
    /// </summary>
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
