using Milvus.Client.V2.Requests.Snapshot;
using Milvus.Client.V2.Responses.Snapshot;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Creates a snapshot of a collection.
    /// </summary>
    /// <remarks>
    /// Flushing the collection before creating the snapshot is recommended so all data is captured in the
    /// snapshot's segment list.
    /// </remarks>
    public async Task CreateSnapshotAsync(
        CreateSnapshotReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.CreateSnapshotRequest grpcRequest = request.ToGrpcCreateSnapshotRequest();
        await InvokeAsync(GrpcClient.CreateSnapshotAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a snapshot permanently.
    /// </summary>
    public async Task DropSnapshotAsync(
        DropSnapshotReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.DropSnapshotRequest grpcRequest = request.ToGrpcDropSnapshotRequest();
        await InvokeAsync(GrpcClient.DropSnapshotAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists the snapshots of a collection, or all snapshots when no collection is given.
    /// </summary>
    public async Task<ListSnapshotsResp> ListSnapshotsAsync(
        ListSnapshotsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.ListSnapshotsRequest grpcRequest = request.ToGrpcListSnapshotsRequest();
        Grpc.ListSnapshotsResponse response = await InvokeAsync(
            GrpcClient.ListSnapshotsAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return ListSnapshotsResp.FromGrpc(response);
    }

    /// <summary>
    /// Gets detailed information about a specific snapshot.
    /// </summary>
    public async Task<DescribeSnapshotResp> DescribeSnapshotAsync(
        DescribeSnapshotReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.DescribeSnapshotRequest grpcRequest = request.ToGrpcDescribeSnapshotRequest();
        Grpc.DescribeSnapshotResponse response = await InvokeAsync(
            GrpcClient.DescribeSnapshotAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return DescribeSnapshotResp.FromGrpc(response);
    }

    /// <summary>
    /// Restores a snapshot into a new (possibly cross-db / cross-collection) collection.
    /// </summary>
    /// <remarks>
    /// This operation is asynchronous and returns a job ID for tracking progress via
    /// <see cref="GetRestoreSnapshotStateAsync" />. The target collection must not already exist.
    /// </remarks>
    public async Task<RestoreSnapshotResp> RestoreSnapshotAsync(
        RestoreSnapshotReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.RestoreSnapshotRequest grpcRequest = request.ToGrpcRestoreSnapshotRequest();
        Grpc.RestoreSnapshotResponse response = await InvokeAsync(
            GrpcClient.RestoreSnapshotAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);

        // The restore creates the target collection server-side. Invalidate its caches like
        // CreateCollectionAsync does: another client may have dropped/recreated the name, so the next
        // describe/insert on the restored collection must see the new schema and ts. Harmless while the
        // async restore job is still running -- the entry is simply dropped and lazily re-loaded.
        InvalidateCollectionCaches(request.TargetCollectionName, ResolveDatabaseName(request.TargetDbName));

        return RestoreSnapshotResp.FromGrpc(response);
    }

    /// <summary>
    /// Queries the status and progress of a restore snapshot job.
    /// </summary>
    public async Task<GetRestoreSnapshotStateResp> GetRestoreSnapshotStateAsync(
        GetRestoreSnapshotStateReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.GetRestoreSnapshotStateRequest grpcRequest = request.ToGrpcGetRestoreSnapshotStateRequest();
        Grpc.GetRestoreSnapshotStateResponse response = await InvokeAsync(
            GrpcClient.GetRestoreSnapshotStateAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return GetRestoreSnapshotStateResp.FromGrpc(response);
    }

    /// <summary>
    /// Lists restore snapshot jobs, optionally filtered by target collection.
    /// </summary>
    public async Task<ListRestoreSnapshotJobsResp> ListRestoreSnapshotJobsAsync(
        ListRestoreSnapshotJobsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.ListRestoreSnapshotJobsRequest grpcRequest = request.ToGrpcListRestoreSnapshotJobsRequest();
        Grpc.ListRestoreSnapshotJobsResponse response = await InvokeAsync(
            GrpcClient.ListRestoreSnapshotJobsAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return ListRestoreSnapshotJobsResp.FromGrpc(response);
    }

    /// <summary>
    /// Pins snapshot data to prevent garbage collection while copying it out (e.g. during backup).
    /// </summary>
    /// <remarks>
    /// Returns a pin ID used to release the pin via <see cref="UnpinSnapshotDataAsync" />.
    /// </remarks>
    public async Task<PinSnapshotDataResp> PinSnapshotDataAsync(
        PinSnapshotDataReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.PinSnapshotDataRequest grpcRequest = request.ToGrpcPinSnapshotDataRequest();
        Grpc.PinSnapshotDataResponse response = await InvokeAsync(
            GrpcClient.PinSnapshotDataAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return PinSnapshotDataResp.FromGrpc(response);
    }

    /// <summary>
    /// Unpins snapshot data, releasing a pin previously allocated by <see cref="PinSnapshotDataAsync" />.
    /// </summary>
    public async Task UnpinSnapshotDataAsync(
        UnpinSnapshotDataReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.UnpinSnapshotDataRequest grpcRequest = request.ToGrpcUnpinSnapshotDataRequest();
        await InvokeAsync(GrpcClient.UnpinSnapshotDataAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }
}
