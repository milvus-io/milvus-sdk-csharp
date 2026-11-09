using System.Text.Json;
using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Utility;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class UtilityApiTests
{

    [Fact]
    public async Task Compact_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.UseDatabaseAsync(
            new UseDatabaseReq { DatabaseName = "db1" },
            TestContext.Current.CancellationToken);

        CompactResp response = await client.CompactAsync(
            new CompactReq
            {
                CollectionName = "coll",
                IsMajorCompaction = true,
                TargetSize = 16
            },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.ManualCompactionRequest)server.Service.Requests["ManualCompaction"];
        Assert.Equal("coll", request.CollectionName);
        Assert.True(request.MajorCompaction);
        Assert.Equal(16, request.TargetSize);
        Assert.Equal(42, response.CompactionId);
        // The database is forwarded via the dbname metadata header (ManualCompactionRequest has no db_name field).
        Assert.Equal("db1", server.Service.LastRequestDbName);
    }

    [Fact]
    public void DumpMessages_returns_lazy_stream_without_invoking_rpc()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        DumpMessagesResp response = client.DumpMessages(
            new DumpMessagesReq
            {
                Pchannel = "by-dev-rootcoord-dml_0",
                StartMessageId = new MessageID("0", WalName.RocksMq),
                StartTimetick = 1,
                EndTimetick = 100,
                IncludeStartMessage = true
            });

        Assert.NotNull(response);
        Assert.DoesNotContain("DumpMessages", server.Service.Requests.Keys);
    }

    [Fact]
    public async Task DumpMessages_wraps_server_cancelled_abort_when_caller_did_not_cancel()
    {
        using var server = new MockMilvusServer { Service = { DumpMessagesCancel = true } };
        using MilvusClientV2 client = server.CreateClient();

        DumpMessagesResp response = client.DumpMessages(
            new DumpMessagesReq
            {
                Pchannel = "by-dev-rootcoord-dml_0",
                StartMessageId = new MessageID("0", WalName.RocksMq),
                StartTimetick = 1,
                EndTimetick = 100,
                IncludeStartMessage = true
            });

        // A server-initiated CANCELLED with the caller's token not cancelled must surface as a MilvusException,
        // not an OperationCanceledException (which would mask the transport failure).
        await Assert.ThrowsAsync<MilvusException>(async () =>
        {
            await foreach (DumpMessageInfo _ in response)
            {
                // no messages expected; the stream aborts on the first move.
            }
        });
    }

    [Fact]
    public async Task FlushAll_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        FlushAllResp response = await client.FlushAllAsync(new FlushAllReq(), TestContext.Current.CancellationToken);

        Assert.Contains("FlushAll", server.Service.Requests.Keys);
        Assert.IsType<Milvus.Client.Grpc.FlushAllRequest>(server.Service.Requests["FlushAll"]);
    }

    [Fact]
    public async Task Flush_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        FlushResp response = await client.FlushAsync(
            new FlushReq { CollectionNames = new[] { "coll", "coll2" } },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.FlushRequest)server.Service.Requests["Flush"];
        Assert.Equal(new[] { "coll", "coll2" }, request.CollectionNames);
    }

    [Fact]
    public async Task Flush_waits_for_flush_state_when_segments_are_reported()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        FlushResp response = await client.FlushAsync(
            new FlushReq { CollectionNames = new[] { "coll" } },
            TestContext.Current.CancellationToken);

        Assert.True(server.Service.Requests.ContainsKey("Flush"));
        Assert.True(server.Service.Requests.ContainsKey("GetFlushState"));
    }

    [Fact]
    public async Task GetCompactionPlans_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        GetCompactionPlansResp response = await client.GetCompactionPlansAsync(
            new GetCompactionPlansReq { CompactionId = 7 },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.GetCompactionPlansRequest)server.Service.Requests["GetCompactionStateWithPlans"];
        Assert.Equal(7, request.CompactionID);
        Assert.Equal(CompactionState.Completed, response.State);
        Assert.Equal(7, response.CompactionId);
    }

    [Fact]
    public async Task ListCompactionTasks_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.UseDatabaseAsync(
            new UseDatabaseReq { DatabaseName = "db1" },
            TestContext.Current.CancellationToken);

        GetCompactionPlansResp response = await client.ListCompactionTasksAsync(
            new ListCompactionTasksReq
            {
                CollectionName = "coll",
                DatabaseName = "db1"
            },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.GetCompactionPlansRequest)server.Service.Requests["GetCompactionStateWithPlans"];
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("db1", request.DbName);
        Assert.Equal("coll", response.CollectionName);
        Assert.Null(response.CompactionId);
        Assert.Equal(CompactionState.Completed, response.State);

        CompactionMergeInfo merge = Assert.Single(response.MergeInfos);
        Assert.Equal(new[] { 1L, 2L }, merge.Sources);
        Assert.Equal(20, merge.Target);
        Assert.Equal(11, merge.PlanId);
        Assert.Equal(12, merge.TriggerId);
        Assert.Equal(13, merge.CollectionId);
        Assert.Equal(14, merge.PartitionId);
        Assert.Equal("by-dev-rootcoord-dml_0", merge.Channel);
        Assert.Equal(CompactionType.Major, merge.CompactionType);
        Assert.Equal(CompactionTaskState.Completed, merge.State);
    }

    [Fact]
    public async Task GetCompactionState_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        GetCompactionStateResp response = await client.GetCompactionStateAsync(
            new GetCompactionStateReq { CompactionId = 42 },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.GetCompactionStateRequest)server.Service.Requests["GetCompactionState"];
        Assert.Equal(42, request.CompactionID);
        Assert.Equal(CompactionState.Completed, response.State);
    }

    [Fact]
    public async Task GetFlushAllState_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        GetFlushAllStateResp response = await client.GetFlushAllStateAsync(
            new GetFlushAllStateReq { FlushAllTimestamp = 100 },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.GetFlushAllStateRequest)server.Service.Requests["GetFlushAllState"];
#pragma warning disable CS0612 // The server marks FlushAllTs as deprecated but still populates it.
        Assert.Equal(100UL, request.FlushAllTs);
#pragma warning restore CS0612
        Assert.True(response.Flushed);
    }

    [Fact]
    public async Task GetMetrics_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.GetMetricsAsync(
            new GetMetricsReq { Request = "{\"metric_type\":\"system_info\"}" },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.GetMetricsRequest)server.Service.Requests["GetMetrics"];
        Assert.Equal("{\"metric_type\":\"system_info\"}", request.Request);
    }

    [Fact]
    public async Task GetPersistentSegmentInfo_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        GetPersistentSegmentInfoResp response = await client.GetPersistentSegmentInfoAsync(
            new GetPersistentSegmentInfoReq { CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.GetPersistentSegmentInfoRequest)server.Service.Requests["GetPersistentSegmentInfo"];
        Assert.Equal("coll", request.CollectionName);
        Assert.Empty(response.Infos);
    }

    [Fact]
    public async Task GetQuerySegmentInfo_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        GetQuerySegmentInfoResp response = await client.GetQuerySegmentInfoAsync(
            new GetQuerySegmentInfoReq { CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.GetQuerySegmentInfoRequest)server.Service.Requests["GetQuerySegmentInfo"];
        Assert.Equal("coll", request.CollectionName);
        Assert.Empty(response.Infos);
    }

    [Fact]
    public async Task GetReplicateConfiguration_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        GetReplicateConfigurationResp response = await client.GetReplicateConfigurationAsync(
            new GetReplicateConfigurationReq(),
            TestContext.Current.CancellationToken);

        Assert.Contains("GetReplicateConfiguration", server.Service.Requests.Keys);
        Assert.IsType<Milvus.Client.Grpc.GetReplicateConfigurationRequest>(server.Service.Requests["GetReplicateConfiguration"]);
        Assert.Empty(response.Configuration.Clusters);
    }

    [Fact]
    public async Task GetReplicateInfo_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        GetReplicateInfoResp response = await client.GetReplicateInfoAsync(
            new GetReplicateInfoReq
            {
                SourceClusterId = "cluster-a",
                TargetPchannel = "by-dev-rootcoord-dml_0"
            },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.GetReplicateInfoRequest)server.Service.Requests["GetReplicateInfo"];
        Assert.Equal("cluster-a", request.SourceClusterId);
        Assert.Equal("by-dev-rootcoord-dml_0", request.TargetPchannel);
        Assert.Null(response.Checkpoint);
    }

    [Fact]
    public async Task Optimize_triggers_compaction()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        OptimizeResp response = await client.OptimizeAsync(
            new OptimizeReq
            {
                CollectionName = "coll",
                TargetSizeInMB = 16,
                WaitForCompletion = true
            },
            TestContext.Current.CancellationToken);

        Assert.Contains("ManualCompaction", server.Service.Requests.Keys);
        var request = (Milvus.Client.Grpc.ManualCompactionRequest)server.Service.Requests["ManualCompaction"];
        Assert.Equal("coll", request.CollectionName);
        Assert.True(request.MajorCompaction);
        Assert.Equal(16, request.TargetSize);
        Assert.Equal(42, response.CompactionId);
        Assert.Equal("16MB", response.TargetSize);
    }

    [Fact]
    public async Task RunAnalyzer_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        RunAnalyzerResp response = await client.RunAnalyzerAsync(
            new RunAnalyzerReq
            {
                // Field mode: uses the collection field's configured analyzer; analyzer_params must stay unset
                // (the server rejects requests that set both).
                Texts = new[] { "hello world" },
                WithDetail = true,
                WithHash = true,
                CollectionName = "coll",
                FieldName = "title"
            },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.RunAnalyzerRequest)server.Service.Requests["RunAnalyzer"];
        Assert.Equal("", request.AnalyzerParams);
        Assert.Single(request.Placeholder);
        Assert.Equal("hello world", request.Placeholder[0].ToStringUtf8());
        Assert.True(request.WithDetail);
        Assert.True(request.WithHash);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("title", request.FieldName);
        Assert.Empty(response.Results);
    }

    [Fact]
    public async Task UpdateReplicateConfiguration_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.UpdateReplicateConfigurationAsync(
            new UpdateReplicateConfigurationReq
            {
                ReplicateConfiguration = new ReplicateConfiguration(
                    new[]
                    {
                        new MilvusCluster("cluster-a", "http://localhost:19530", "token", new[] { "by-dev-rootcoord-dml_0" })
                    },
                    new[] { new CrossClusterTopology("cluster-a", "cluster-b") })
            },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.UpdateReplicateConfigurationRequest)server.Service.Requests["UpdateReplicateConfiguration"];
        Assert.NotNull(request.ReplicateConfiguration);
        Assert.Single(request.ReplicateConfiguration.Clusters);
        Assert.Equal("cluster-a", request.ReplicateConfiguration.Clusters[0].ClusterId);
        Assert.Single(request.ReplicateConfiguration.CrossClusterTopology);
        Assert.Equal("cluster-b", request.ReplicateConfiguration.CrossClusterTopology[0].TargetClusterId);
    }

    [Fact]
    public async Task UseDatabase_switches_default_database()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.UseDatabaseAsync(
            new UseDatabaseReq { DatabaseName = "db1" },
            TestContext.Current.CancellationToken);

        await client.HasCollectionAsync(new HasCollectionReq { CollectionName = "coll" }, TestContext.Current.CancellationToken);

        // The dbname header must be sent on the following call, proving the client switched databases.
        Assert.Equal("coll", server.Service.LastCheckedCollectionName);
        Assert.Equal("db1", server.Service.LastRequestDbName);
    }

    [Fact]
    public async Task GetClientTelemetry_forwards_request_and_maps_response()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var telemetryClient = new Milvus.Client.Grpc.ClientTelemetry
        {
            ClientInfo = new Milvus.Client.Grpc.ClientInfo { SdkType = "CSharp", SdkVersion = "3.0.0" },
            LastHeartbeatTime = 42,
            Status = "healthy"
        };
        telemetryClient.Databases.Add("default");
        server.TelemetryService.Response = new Milvus.Client.Grpc.GetClientTelemetryResponse
        {
            Status = new Milvus.Client.Grpc.Status(),
            Aggregated = new Milvus.Client.Grpc.Metrics { RequestCount = 7, SuccessCount = 7 }
        };
        server.TelemetryService.Response.Clients.Add(telemetryClient);

        GetClientTelemetryResp response = await client.GetClientTelemetryAsync(
            new GetClientTelemetryReq
            {
                Database = "default",
                ClientId = "client-1",
                IncludeMetrics = true
            },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.GetClientTelemetryRequest)server.TelemetryService.Requests["GetClientTelemetry"];
        Assert.Equal("default", request.Database);
        Assert.Equal("client-1", request.ClientId);
        Assert.True(request.IncludeMetrics);

        Assert.Equal(7, response.Aggregated.RequestCount);
        ClientTelemetryInfo info = Assert.Single(response.Clients);
        Assert.Equal("CSharp", info.ClientInfo.SdkType);
        Assert.Equal("3.0.0", info.ClientInfo.SdkVersion);
        Assert.Equal("healthy", info.Status);
        Assert.Equal(new[] { "default" }, info.Databases);
    }

    [Fact]
    public async Task ListRunningRequests_forwards_request_and_maps_response()
    {
        using var server = new MockMilvusServer
        {
            Service =
            {
                RunningRequests =
                {
                    new Milvus.Client.Grpc.RunningRequestInfo
                    {
                        RequestId = "25-to03zu-rs",
                        ProxyId = 25,
                        Type = "Search",
                        DbName = "default",
                        CollectionName = "docs",
                        User = "alice",
                        ClientAddr = "10.0.0.1:55000",
                        Nq = 100,
                        Topk = 10,
                        Expr = "title like \"milvus\"",
                        StartTimeMs = 1000,
                        QueuedMs = 5,
                        ElapsedMs = 42,
                        State = "Running",
                        TraceId = "trace-1",
                        Cancellable = true
                    },
                    new Milvus.Client.Grpc.RunningRequestInfo
                    {
                        RequestId = "25-to03zu-rt",
                        ProxyId = 25,
                        Type = "Query",
                        DbName = "default",
                        CollectionName = "docs",
                        User = "bob",
                        ClientAddr = "10.0.0.2:55000",
                        Nq = 0,
                        Topk = 0,
                        Expr = "",
                        StartTimeMs = 2000,
                        QueuedMs = 0,
                        ElapsedMs = 10,
                        State = "Queued"
                    }
                },
                RunningRequestNodeResults =
                {
                    new Milvus.Client.Grpc.RunningRequestNodeResult
                    {
                        NodeId = 25,
                        Status = new Milvus.Client.Grpc.Status()
                    },
                    new Milvus.Client.Grpc.RunningRequestNodeResult
                    {
                        NodeId = 26,
                        Status = new Milvus.Client.Grpc.Status { Code = 65535, Reason = "timeout" }
                    }
                }
            }
        };
        using MilvusClientV2 client = server.CreateClient();

        ListRunningRequestsResp response = await client.ListRunningRequestsAsync(
            new ListRunningRequestsReq
            {
                DatabaseName = "default",
                CollectionName = "docs",
                User = "alice",
                MinElapsedMs = 5000
            },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.ListRunningRequestsRequest)server.Service.Requests["ListRunningRequests"];
        Assert.Equal("default", request.DbName);
        Assert.Equal("docs", request.CollectionName);
        Assert.Equal("alice", request.User);
        Assert.Equal(5000, request.MinElapsedMs);

        Assert.Equal(2, response.Requests.Count);
        RunningRequestInfo first = response.Requests[0];
        Assert.Equal("25-to03zu-rs", first.RequestId);
        Assert.Equal(25, first.ProxyId);
        Assert.Equal("Search", first.Type);
        Assert.Equal("default", first.DatabaseName);
        Assert.Equal("docs", first.CollectionName);
        Assert.Equal("alice", first.User);
        Assert.Equal("10.0.0.1:55000", first.ClientAddr);
        Assert.Equal(100, first.Nq);
        Assert.Equal(10, first.TopK);
        Assert.Equal("title like \"milvus\"", first.Expr);
        Assert.Equal(1000, first.StartTimeMs);
        Assert.Equal(5, first.QueuedMs);
        Assert.Equal(42, first.ElapsedMs);
        Assert.Equal("Running", first.State);
        Assert.Empty(first.TaskIds);
        Assert.Equal("trace-1", first.TraceId);
        Assert.True(first.Cancellable);

        RunningRequestInfo second = response.Requests[1];
        Assert.Equal("25-to03zu-rt", second.RequestId);
        Assert.Empty(second.TaskIds);
        Assert.False(second.Cancellable);

        Assert.Equal(2, response.NodeResults.Count);
        Assert.Equal(25, response.NodeResults[0].NodeId);
        Assert.True(response.NodeResults[0].IsSuccess);
        Assert.Equal(0, response.NodeResults[0].ErrorCode);
        Assert.False(response.NodeResults[0].Unimplemented);
        Assert.Equal(26, response.NodeResults[1].NodeId);
        Assert.Equal(65535, response.NodeResults[1].ErrorCode);
        Assert.Equal("timeout", response.NodeResults[1].Reason);
        Assert.False(response.NodeResults[1].IsSuccess);
    }

    [Fact]
    public async Task ListRunningRequests_no_filters_forwards_empty_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.ListRunningRequestsAsync(new ListRunningRequestsReq(), TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.ListRunningRequestsRequest)server.Service.Requests["ListRunningRequests"];
        Assert.Equal("", request.DbName);
        Assert.Equal("", request.CollectionName);
        Assert.Equal("", request.User);
        Assert.Equal(0, request.MinElapsedMs);
    }

    [Fact]
    public async Task CancelRequests_forwards_request_and_maps_response()
    {
        using var server = new MockMilvusServer
        {
            Service =
            {
                RunningRequests =
                {
                    new Milvus.Client.Grpc.RunningRequestInfo
                    {
                        RequestId = "25-to03zu-rs",
                        ProxyId = 25,
                        Type = "Search",
                        DbName = "default",
                        CollectionName = "docs",
                        User = "alice",
                        Nq = 100,
                        Topk = 10,
                        ElapsedMs = 42,
                        State = "Running",
                        Cancellable = true
                    }
                },
                CancelNotFound = { "25-to03zu-xx" },
                CancelUndetermined = { "25-to03zu-yy" },
                RunningRequestNodeResults =
                {
                    new Milvus.Client.Grpc.RunningRequestNodeResult { NodeId = 25, Status = new Milvus.Client.Grpc.Status() }
                }
            }
        };
        using MilvusClientV2 client = server.CreateClient();

        CancelRequestsResp response = await client.CancelRequestsAsync(
            new CancelRequestsReq
            {
                RequestIds = new[] { "25-to03zu-rs", "25-to03zu-xx" },
                Reason = "too heavy",
                User = "alice"
            },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.CancelRequestsRequest)server.Service.Requests["CancelRequests"];
        Assert.Equal(new[] { "25-to03zu-rs", "25-to03zu-xx" }, request.RequestIds);
        Assert.Equal("too heavy", request.Reason);
        Assert.Equal("alice", request.User);

        RunningRequestInfo canceled = Assert.Single(response.Canceled);
        Assert.Equal("25-to03zu-rs", canceled.RequestId);
        Assert.Equal(42, canceled.ElapsedMs);
        Assert.Equal("Running", canceled.State);
        Assert.Equal(new[] { "25-to03zu-xx" }, response.NotFound);
        Assert.Equal(new[] { "25-to03zu-yy" }, response.Undetermined);
        RunningRequestNodeResult nodeResult = Assert.Single(response.NodeResults);
        Assert.Equal(25, nodeResult.NodeId);
        Assert.True(nodeResult.IsSuccess);
    }

    [Fact]
    public async Task CancelRequests_rejects_empty_id_list_without_an_rpc()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.CancelRequestsAsync(
                new CancelRequestsReq { RequestIds = Array.Empty<string>() },
                TestContext.Current.CancellationToken));

        Assert.DoesNotContain("CancelRequests", server.Service.Requests.Keys);
    }

    [Fact]
    public async Task CancelRequests_maps_unimplemented_node_result()
    {
        using var server = new MockMilvusServer
        {
            Service =
            {
                RunningRequests =
                {
                    new Milvus.Client.Grpc.RunningRequestInfo
                    {
                        RequestId = "25-to03zu-rs",
                        ProxyId = 25,
                        Type = "Search",
                        Cancellable = true
                    }
                },
                RunningRequestNodeResults =
                {
                    new Milvus.Client.Grpc.RunningRequestNodeResult
                    {
                        NodeId = 26,
                        Status = new Milvus.Client.Grpc.Status { Code = 10, Reason = "not implemented" },
                        Unimplemented = true
                    }
                }
            }
        };
        using MilvusClientV2 client = server.CreateClient();

        CancelRequestsResp response = await client.CancelRequestsAsync(
            new CancelRequestsReq { RequestIds = new[] { "25-to03zu-rs" } },
            TestContext.Current.CancellationToken);

        RunningRequestNodeResult nodeResult = Assert.Single(response.NodeResults);
        Assert.Equal(26, nodeResult.NodeId);
        Assert.Equal(10, nodeResult.ErrorCode);
        Assert.Equal("not implemented", nodeResult.Reason);
        Assert.True(nodeResult.Unimplemented);
        Assert.False(nodeResult.IsSuccess);
    }

}
