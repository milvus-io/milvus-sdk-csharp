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

}
