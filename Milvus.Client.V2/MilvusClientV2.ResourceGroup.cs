using Milvus.Client.V2.Requests.ResourceGroup;
using Milvus.Client.V2.Responses.ResourceGroup;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Creates a resource group.
    /// </summary>
    public async Task CreateResourceGroupAsync(CreateResourceGroupReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.CreateResourceGroupRequest grpcRequest = request.ToGrpcCreateResourceGroupRequest();
        await InvokeAsync(GrpcClient.CreateResourceGroupAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a resource group.
    /// </summary>
    public async Task DropResourceGroupAsync(DropResourceGroupReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.DropResourceGroupRequest grpcRequest = request.ToGrpcDropResourceGroupRequest();
        await InvokeAsync(GrpcClient.DropResourceGroupAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates resource groups.
    /// </summary>
    public async Task UpdateResourceGroupsAsync(UpdateResourceGroupsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.UpdateResourceGroupsRequest grpcRequest = request.ToGrpcUpdateResourceGroupsRequest();
        await InvokeAsync(GrpcClient.UpdateResourceGroupsAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Transfers a number of nodes from one resource group to another.
    /// </summary>
    public async Task TransferNodeAsync(TransferNodeReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.TransferNodeRequest grpcRequest = request.ToGrpcTransferNodeRequest();
        await InvokeAsync(GrpcClient.TransferNodeAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Transfers replicas from one resource group to another.
    /// </summary>
    public async Task TransferReplicaAsync(TransferReplicaReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.TransferReplicaRequest grpcRequest = request.ToGrpcTransferReplicaRequest();
        await InvokeAsync(GrpcClient.TransferReplicaAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists all resource groups.
    /// </summary>
    public async Task<ListResourceGroupsResp> ListResourceGroupsAsync(
        ListResourceGroupsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.ListResourceGroupsRequest grpcRequest = ListResourceGroupsReq.ToGrpcListResourceGroupsRequest();
        Grpc.ListResourceGroupsResponse response = await InvokeAsync(
            GrpcClient.ListResourceGroupsAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return ListResourceGroupsResp.FromGrpc(response);
    }

    /// <summary>
    /// Describes a resource group.
    /// </summary>
    public async Task<DescribeResourceGroupResp> DescribeResourceGroupAsync(
        DescribeResourceGroupReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.DescribeResourceGroupRequest grpcRequest = request.ToGrpcDescribeResourceGroupRequest();
        Grpc.DescribeResourceGroupResponse response = await InvokeAsync(
            GrpcClient.DescribeResourceGroupAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return DescribeResourceGroupResp.FromGrpc(response);
    }
}
