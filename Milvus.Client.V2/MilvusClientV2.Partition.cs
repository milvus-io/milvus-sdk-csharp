using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Partition;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Responses.Partition;
using Milvus.Client.V2.Types;
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

        if (!request.Sync)
        {
            return;
        }

        DateTime? deadline = request.TimeoutMs > 0
            ? DateTime.UtcNow + TimeSpan.FromMilliseconds(request.TimeoutMs)
            : null;

        while (true)
        {
            GetLoadStateResp state = await GetLoadStateAsync(
                new GetLoadStateReq { DatabaseName = request.DatabaseName, CollectionName = request.CollectionName, PartitionNames = request.PartitionNames },
                cancellationToken).ConfigureAwait(false);

            if (state.State == LoadState.Loaded)
            {
                return;
            }

            if (state.State is LoadState.NotExist or LoadState.NotLoad)
            {
                throw new MilvusException(
                    MilvusErrorCode.UnexpectedError,
                    $"Collection '{request.CollectionName}' is in {state.State} state; load did not complete.");
            }

            if (deadline is not null && DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Timed out waiting for collection '{request.CollectionName}' to load.");
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }
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
        return GetPartitionStatsResp.FromGrpc(request.PartitionName, response);
    }
}
