using Milvus.Client.V2.Requests.Partition;
using Milvus.Client.V2.Responses.Partition;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Creates a partition in a collection.
    /// </summary>
    public async Task CreatePartitionAsync(
        CreatePartitionReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.CreatePartitionRequest grpcRequest = request.ToGrpcCreatePartitionRequest();
        await InvokeAsync(GrpcClient.CreatePartitionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a partition from a collection.
    /// </summary>
    public async Task DropPartitionAsync(
        DropPartitionReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.DropPartitionRequest grpcRequest = request.ToGrpcDropPartitionRequest();
        await InvokeAsync(GrpcClient.DropPartitionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks whether a partition exists in a collection.
    /// </summary>
    public async Task<HasPartitionResp> HasPartitionAsync(
        HasPartitionReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.HasPartitionRequest grpcRequest = request.ToGrpcHasPartitionRequest();
        Grpc.BoolResponse response = await InvokeAsync(
            GrpcClient.HasPartitionAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return HasPartitionResp.FromGrpc(response);
    }

    /// <summary>
    /// Lists the partitions of a collection.
    /// </summary>
    public async Task<ListPartitionsResp> ListPartitionsAsync(
        ListPartitionsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.ShowPartitionsRequest grpcRequest = request.ToGrpcShowPartitionsRequest();
        Grpc.ShowPartitionsResponse response = await InvokeAsync(
            GrpcClient.ShowPartitionsAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return ListPartitionsResp.FromGrpc(response);
    }

    /// <summary>
    /// Loads specific partitions into memory.
    /// </summary>
    public async Task LoadPartitionsAsync(
        LoadPartitionsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.LoadPartitionsRequest grpcRequest = request.ToGrpcLoadPartitionsRequest();
        await InvokeAsync(GrpcClient.LoadPartitionsAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Releases specific partitions from memory.
    /// </summary>
    public async Task ReleasePartitionsAsync(
        ReleasePartitionsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.ReleasePartitionsRequest grpcRequest = request.ToGrpcReleasePartitionsRequest();
        await InvokeAsync(GrpcClient.ReleasePartitionsAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the statistics of a partition.
    /// </summary>
    public async Task<GetPartitionStatsResp> GetPartitionStatsAsync(
        GetPartitionStatsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.GetPartitionStatisticsRequest grpcRequest = request.ToGrpcGetPartitionStatisticsRequest();
        Grpc.GetPartitionStatisticsResponse response = await InvokeAsync(
            GrpcClient.GetPartitionStatisticsAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return GetPartitionStatsResp.FromGrpc(response);
    }
}
