using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.ResourceGroup;

/// <summary>
/// Represents a request to transfer replicas of a collection from one resource group to another.
/// </summary>
public sealed class TransferReplicaReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the resource group to transfer replicas from.
    /// </summary>
    public string SourceResourceGroup { get; set; } = "";

    /// <summary>
    /// The name of the resource group to transfer replicas to.
    /// </summary>
    public string TargetResourceGroup { get; set; } = "";

    /// <summary>
    /// The name of the collection whose replicas are to be transferred.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The number of replicas to transfer.
    /// </summary>
    public long NumReplica { get; set; }
    internal Grpc.TransferReplicaRequest ToGrpcTransferReplicaRequest()
    {
        Verify.NotNullOrWhiteSpace(SourceResourceGroup);
        Verify.NotNullOrWhiteSpace(TargetResourceGroup);
        Verify.NotNullOrWhiteSpace(CollectionName);
        var request = new Grpc.TransferReplicaRequest
        {
            SourceResourceGroup = SourceResourceGroup,
            TargetResourceGroup = TargetResourceGroup,
            CollectionName = CollectionName,
            NumReplica = NumReplica
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
