using Milvus.Client.V2.Responses.Collection;

using Grpc.Core;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Index;
using Milvus.Client.V2.Responses.Utility;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Compacts a collection (merging segments), returning the compaction id.
    /// </summary>
    public async Task<CompactResp> CompactAsync(CompactReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        DescribeCollectionResp description = await DescribeCollectionAsync(
            new Requests.Collection.DescribeCollectionReq
            {
                DatabaseName = request.DatabaseName,
                CollectionName = request.CollectionName
            },
            cancellationToken).ConfigureAwait(false);

        Grpc.ManualCompactionRequest grpcRequest = request.ToGrpcManualCompactionRequest(description.CollectionId);
        Grpc.ManualCompactionResponse response = await InvokeAsync(
            GrpcClient.ManualCompactionAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return CompactResp.FromGrpc(response);
    }

    /// <summary>
    /// Gets the state of a compaction.
    /// </summary>
    public async Task<GetCompactionStateResp> GetCompactionStateAsync(
        GetCompactionStateReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.GetCompactionStateRequest grpcRequest = request.ToGrpcGetCompactionStateRequest();
        Grpc.GetCompactionStateResponse response = await InvokeAsync(
            GrpcClient.GetCompactionStateAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return GetCompactionStateResp.FromGrpc(response);
    }

    /// <summary>
    /// Gets the compaction plans for a compaction.
    /// </summary>
    public async Task<GetCompactionPlansResp> GetCompactionPlansAsync(
        GetCompactionPlansReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.GetCompactionPlansRequest grpcRequest = request.ToGrpcGetCompactionPlansRequest();
        Grpc.GetCompactionPlansResponse response = await InvokeAsync(
            GrpcClient.GetCompactionStateWithPlansAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return GetCompactionPlansResp.FromGrpc(response);
    }

    /// <summary>
    /// Flushes the given collections, optionally waiting for the flush to complete.
    /// </summary>
    public async Task<FlushResp> FlushAsync(FlushReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.FlushRequest grpcRequest = request.ToGrpcFlushRequest();
        Grpc.FlushResponse response = await InvokeAsync(
            GrpcClient.FlushAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);

        FlushResp flushResp = FlushResp.FromGrpc(response);
        if (flushResp.CollSegIDs.Count == 0)
        {
            return flushResp;
        }

        // Wait for the segments to be flushed, mirroring the C++ SDK: poll GetFlushState every 500 ms until all
        // segments are flushed or the WaitFlushedMs timeout elapses (0 means wait forever).
        DateTime? deadline = request.WaitFlushedMs > 0
            ? DateTime.UtcNow + TimeSpan.FromMilliseconds(request.WaitFlushedMs)
            : null;

        var remaining = new Dictionary<string, IReadOnlyList<long>>();
        foreach (KeyValuePair<string, IReadOnlyList<long>> pair in flushResp.CollSegIDs)
        {
            remaining[pair.Key] = pair.Value;
        }

        while (remaining.Count > 0)
        {
            foreach (KeyValuePair<string, IReadOnlyList<long>> pair in remaining.ToList())
            {
                // Pass the collection's flush timestamp so the datacoord waits for the WAL checkpoint to
                // catch up before reporting flushed (C++ SDK passes coll_flush_ts in the same loop).
                flushResp.CollFlushTs.TryGetValue(pair.Key, out ulong flushTs);
                GetFlushStateResp state = await GetFlushStateAsync(
                    new GetFlushStateReq { DatabaseName = request.DatabaseName, CollectionName = pair.Key, SegmentIds = pair.Value, FlushTs = flushTs },
                    cancellationToken).ConfigureAwait(false);

                if (state.Flushed)
                {
                    remaining.Remove(pair.Key);
                }
            }

            if (remaining.Count == 0)
            {
                break;
            }

            if (deadline is not null && DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Timed out waiting for the flush to complete.");
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return flushResp;
    }

    /// <summary>
    /// Checks whether the given segments have been flushed.
    /// </summary>
    public async Task<GetFlushStateResp> GetFlushStateAsync(
        GetFlushStateReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.GetFlushStateRequest grpcRequest = request.ToGrpcGetFlushStateRequest();
        Grpc.GetFlushStateResponse response = await InvokeAsync(
            GrpcClient.GetFlushStateAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return GetFlushStateResp.FromGrpc(response);
    }

    /// <summary>
    /// Gets the persistent segment info of a collection.
    /// </summary>
    public async Task<GetPersistentSegmentInfoResp> GetPersistentSegmentInfoAsync(
        GetPersistentSegmentInfoReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.GetPersistentSegmentInfoRequest grpcRequest = request.ToGrpcGetPersistentSegmentInfoRequest();
        Grpc.GetPersistentSegmentInfoResponse response = await InvokeAsync(
            GrpcClient.GetPersistentSegmentInfoAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return GetPersistentSegmentInfoResp.FromGrpc(response, request.CollectionName);
    }

    /// <summary>
    /// Flushes all collections.
    /// </summary>
    public async Task<FlushAllResp> FlushAllAsync(FlushAllReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.FlushAllRequest grpcRequest = request.ToGrpcFlushAllRequest();
        Grpc.FlushAllResponse response = await InvokeAsync(
            GrpcClient.FlushAllAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);

        FlushAllResp result = FlushAllResp.FromGrpc(response);

        // When WaitFlushedMs is set, poll GetFlushAllState until the flush completes or the window expires,
        // mirroring the C++ SDK's wait_for_status loop.
        if (request.WaitFlushedMs > 0)
        {
            DateTime deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(request.WaitFlushedMs);
            while (true)
            {
                GetFlushAllStateResp state = await GetFlushAllStateAsync(
                    new GetFlushAllStateReq
                    {
                        DatabaseName = request.DatabaseName,
                        FlushAllTimestamp = result.FlushAllTimestamp
                    },
                    cancellationToken).ConfigureAwait(false);
                if (state.Flushed)
                {
                    break;
                }

                if (DateTime.UtcNow >= deadline)
                {
                    throw new MilvusException(
                        MilvusErrorCode.UnexpectedError,
                        $"Timed out waiting for flush-all to complete after {request.WaitFlushedMs} ms.");
                }

                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the state of a flush-all operation.
    /// </summary>
    public async Task<GetFlushAllStateResp> GetFlushAllStateAsync(
        GetFlushAllStateReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.GetFlushAllStateRequest grpcRequest = request.ToGrpcGetFlushAllStateRequest();
        Grpc.GetFlushAllStateResponse response = await InvokeAsync(
            GrpcClient.GetFlushAllStateAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return GetFlushAllStateResp.FromGrpc(response);
    }

    /// <summary>
    /// Gets the loaded query-segment info of a collection.
    /// </summary>
    public async Task<GetQuerySegmentInfoResp> GetQuerySegmentInfoAsync(
        GetQuerySegmentInfoReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.GetQuerySegmentInfoRequest grpcRequest = request.ToGrpcGetQuerySegmentInfoRequest();
        Grpc.GetQuerySegmentInfoResponse response = await InvokeAsync(
            GrpcClient.GetQuerySegmentInfoAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return GetQuerySegmentInfoResp.FromGrpc(response, request.CollectionName);
    }

    /// <summary>
    /// Runs the text analyzer on the given strings, returning the analyzed tokens.
    /// </summary>
    public async Task<RunAnalyzerResp> RunAnalyzerAsync(
        RunAnalyzerReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.RunAnalyzerRequest grpcRequest = request.ToGrpcRunAnalyzerRequest();
        Grpc.RunAnalyzerResponse response = await InvokeAsync(
            GrpcClient.RunAnalyzerAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return RunAnalyzerResp.FromGrpc(response);
    }

    /// <summary>
    /// Gets the metrics of the Milvus server.
    /// </summary>
    public async Task<GetMetricsResp> GetMetricsAsync(GetMetricsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.GetMetricsRequest grpcRequest = request.ToGrpcGetMetricsRequest();
        Grpc.GetMetricsResponse response = await InvokeAsync(
            GrpcClient.GetMetricsAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return GetMetricsResp.FromGrpc(response);
    }

    /// <summary>
    /// Gets replication info about a physical channel.
    /// </summary>
    public async Task<GetReplicateInfoResp> GetReplicateInfoAsync(
        GetReplicateInfoReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.GetReplicateInfoRequest grpcRequest = request.ToGrpcRequest();
        try
        {
            Grpc.GetReplicateInfoResponse response = await RetryPolicy.ExecuteAsync(
                async innerCt => await GrpcClient.GetReplicateInfoAsync(
                        grpcRequest, CreateCallOptions(innerCt)).ConfigureAwait(false),
                _retryConfig,
                cancellationToken).ConfigureAwait(false);
            return GetReplicateInfoResp.FromGrpc(response);
        }
        catch (RpcException ex)
        {
            // GetReplicateInfoResponse carries no status field, so it cannot go through InvokeAsync; apply the
            // same CreateCallOptions deadline and RpcException->MilvusException wrapping for a consistent surface,
            // preserving the original gRPC status code.
            throw new MilvusException(
                MilvusErrorCode.UnexpectedError, $"RPC failed: {ex.StatusCode} {ex.Status.Detail}",
                ex.StatusCode, ex);
        }
    }

    /// <summary>
    /// Updates the replication configuration of a cluster.
    /// </summary>
    public async Task<UpdateReplicateConfigurationResp> UpdateReplicateConfigurationAsync(
        UpdateReplicateConfigurationReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.UpdateReplicateConfigurationRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.UpdateReplicateConfigurationAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
        return UpdateReplicateConfigurationResp.FromGrpc();
    }

    /// <summary>
    /// Switches the default database for this client.
    /// </summary>
    /// <remarks>
    /// This is a synchronous client-side state switch: no RPC is issued, the switch applies immediately, and
    /// the returned task is already completed. The <c>Async</c> suffix is kept for a uniform public surface
    /// (the Java and C++ SDKs expose the same synchronous <c>useDatabase</c>/<c>UseDatabase</c>).
    /// </remarks>
    public Task UseDatabaseAsync(UseDatabaseReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        request.Validate();

        // Guard the header/database swap so a concurrent UseDatabaseAsync cannot interleave the dbname
        // metadata header with the CurrentDatabase cache key (which would make RPCs hit one database while
        // session-consistency reads consult another).
        lock (_connectLock)
        {
            string? authorization = _authorizationHeader;

            var metadata = new Metadata();
            if (authorization is not null)
            {
                metadata.Add("authorization", authorization);
            }

            metadata.Add("dbname", request.DatabaseName);
            _callOptions = _callOptions.WithHeaders(metadata);
            _database = request.DatabaseName;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the replication configuration of a cluster.
    /// </summary>
    public async Task<GetReplicateConfigurationResp> GetReplicateConfigurationAsync(
        GetReplicateConfigurationReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.GetReplicateConfigurationRequest grpcRequest = GetReplicateConfigurationReq.ToGrpcRequest();
        Grpc.GetReplicateConfigurationResponse response = await InvokeAsync(
            GrpcClient.GetReplicateConfigurationAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return GetReplicateConfigurationResp.FromGrpc(response);
    }

    /// <summary>
    /// Dumps CDC messages from a physical channel, as a stream of <see cref="DumpMessageInfo" />.
    /// </summary>
    /// <remarks>
    /// Named <c>DumpMessages</c> (not <c>...Async</c>) because it returns the lazy stream immediately rather
    /// than a <see cref="Task" />, matching the Java SDK's <c>dumpMessages</c>. Consume with
    /// <c>await foreach</c>, optionally with <c>.WithCancellation(token)</c>.
    /// </remarks>
    public DumpMessagesResp DumpMessages(DumpMessagesReq request)
    {
        Verify.NotNull(request);
        Grpc.DumpMessagesRequest grpcRequest = request.ToGrpcRequest();
        return new DumpMessagesResp(ct => DumpMessagesReader.ReadAsync(this, grpcRequest, ct));
    }

    /// <summary>
    /// Optimizes a collection (rebuilding indexes and compacting segments).
    /// </summary>
    /// <param name="request">The optimize request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<OptimizeResp> OptimizeAsync(OptimizeReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        request.Validate();
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        DateTime? deadline = request.TimeoutMilliseconds is { } timeout
            ? DateTime.UtcNow + TimeSpan.FromMilliseconds(timeout)
            : null;

        var progress = new List<string> { "waiting for indexes" };

        // Wait for all indexes to be built.
        await WaitForIndexesAsync(request.CollectionName, request.DatabaseName, deadline, cancellationToken).ConfigureAwait(false);

        // Trigger a major compaction with the optional target size. The free-form TargetSize string (e.g.
        // "1GB") is split into a number + unit for the CompactReq, mirroring the Java SDK's targetSize.
        long? targetSizeMB = request.TargetSizeInMB;
        string? targetSizeUnit = null;
        if (request.TargetSize is not null)
        {
            if (request.TargetSizeInMB is not null)
            {
                throw new ArgumentException(
                    "TargetSize and TargetSizeInMB are mutually exclusive.", nameof(request));
            }

            (targetSizeMB, targetSizeUnit) = ParseTargetSize(request.TargetSize);
        }

        CompactResp compact = await CompactAsync(new CompactReq
        {
            DatabaseName = request.DatabaseName,
            CollectionName = request.CollectionName,
            IsMajorCompaction = true,
            TargetSize = targetSizeMB,
            TargetSizeUnit = targetSizeUnit
        }, cancellationToken).ConfigureAwait(false);

        progress.Add("compacting");

        // The datacoord returns CompactionID = -1 when a manual compaction creates no tasks (e.g. an empty
        // collection or all segments already under the target size); normalize it to null so callers do not
        // persist or poll a bogus -1.
        long? compactionId = compact.CompactionId < 0 ? null : compact.CompactionId;

        if (request.WaitForCompletion && compactionId is not null)
        {
            progress.Add("waiting for compaction");

            // Wait for the compaction to complete.
            while (true)
            {
                GetCompactionStateResp state = await GetCompactionStateAsync(
                    new GetCompactionStateReq { CompactionId = compactionId.Value },
                    cancellationToken).ConfigureAwait(false);
                if (state.State == CompactionState.Completed)
                {
                    break;
                }

                if (deadline is not null && DateTime.UtcNow >= deadline)
                {
                    throw new TimeoutException($"Timed out waiting for compaction '{compactionId.Value}' of collection '{request.CollectionName}' to complete.");
                }

                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }

            // Wait for the indexes to be rebuilt after compaction.
            progress.Add("waiting for index rebuild");
            await WaitForIndexesAsync(request.CollectionName, request.DatabaseName, deadline, cancellationToken).ConfigureAwait(false);

            // Refresh the loaded data if the collection is loaded.
            GetLoadStateResp loadState = await GetLoadStateAsync(
                new Requests.Collection.GetLoadStateReq { DatabaseName = request.DatabaseName, CollectionName = request.CollectionName },
                cancellationToken).ConfigureAwait(false);
            if (loadState.State == LoadState.Loaded)
            {
                progress.Add("refreshing load");
                await RefreshLoadAsync(new Requests.Collection.RefreshLoadReq
                {
                    DatabaseName = request.DatabaseName,
                    CollectionName = request.CollectionName,
                    Sync = true,
                    TimeoutMilliseconds = request.TimeoutMilliseconds
                }, cancellationToken).ConfigureAwait(false);
            }
        }

        return new OptimizeResp(
            "success",
            request.CollectionName,
            compactionId,
            request.TargetSize is not null
                ? request.TargetSize
                : request.TargetSizeInMB is { } size ? size + "MB" : null,
            progress);
    }

    // Splits a free-form target-size string ("512MB", "1GB") into (value, unit), mirroring the Java SDK's
    // OptimizeReq.targetSize. The value/unit are passed to CompactReq, which converts to MB.
    private static (long Value, string Unit) ParseTargetSize(string targetSize)
    {
        string trimmed = targetSize.Trim();
        int i = 0;
        while (i < trimmed.Length && char.IsDigit(trimmed[i]))
        {
            i++;
        }

        if (i == 0 || i == trimmed.Length)
        {
            throw new ArgumentException(
                $"Invalid targetSize '{targetSize}'; expected a number with a unit (e.g. \"512MB\", \"1GB\").");
        }

        // net8.0 wants AsSpan, netstandard2.0/net462 has no span overloads; substring is required to stay
        // multi-target, so the CA1846 suggestion is intentionally not followed here.
#pragma warning disable CA1846
        long value = long.Parse(trimmed.Substring(0, i), System.Globalization.CultureInfo.InvariantCulture);
#pragma warning restore CA1846
#pragma warning disable CA1308, CA1846 // The unit contract is lowercase; the string overload is required for netstandard2.0/net462.
        string unit = trimmed.Substring(i).Trim().ToLowerInvariant();
#pragma warning restore CA1308, CA1846
        if (unit is not ("b" or "kb" or "mb" or "gb" or "tb" or "pb"))
        {
            throw new ArgumentException(
                $"Invalid targetSize unit '{unit}'; supported units: b, kb, mb, gb, tb, pb");
        }

        return (value, unit);
    }

    private async Task WaitForIndexesAsync(
        string collectionName, string? databaseName, DateTime? deadline, CancellationToken cancellationToken)
    {
        while (true)
        {
            ListIndexesResp indexes = await ListIndexesAsync(
                new ListIndexesReq { DatabaseName = databaseName, CollectionName = collectionName },
                cancellationToken).ConfigureAwait(false);

            bool allFinished = true;
            foreach (IndexDesc index in indexes.Indexes)
            {
                if (index.State == IndexState.Failed)
                {
                    throw new MilvusException(MilvusErrorCode.UnexpectedError,
                        $"Index build failed for '{index.IndexName}' on collection '{collectionName}': {index.IndexStateFailReason}");
                }

                if (index.State != IndexState.Finished)
                {
                    allFinished = false;
                }
            }

            if (allFinished)
            {
                return;
            }

            if (deadline is not null && DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Timed out waiting for indexes of collection '{collectionName}' to finish.");
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }
    }
}
