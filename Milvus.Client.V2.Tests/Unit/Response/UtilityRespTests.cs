using System.Text;
using Xunit;

using Google.Protobuf;

using Milvus.Client.V2.Responses.Utility;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Response;

[Trait("Category", "Unit")]
public class UtilityRespTests
{
    [Fact]
    public void AnalyzerToken_maps_token_and_offsets()
    {
        var token = new Grpc.AnalyzerToken { Token = "milvus", StartOffset = 1, EndOffset = 7, Position = 0, PositionLength = 3, Hash = 0xABCD1234 };

        AnalyzerToken result = AnalyzerToken.FromGrpc(token);

        Assert.Equal("milvus", result.Token);
        Assert.Equal(1, result.StartOffset);
        Assert.Equal(7, result.EndOffset);
        Assert.Equal(0, result.Position);
        Assert.Equal(3, result.PositionLength);
        Assert.Equal(0xABCD1234u, result.Hash);
    }

    [Fact]
    public void AnalyzerResult_maps_tokens()
    {
        var grpc = new Grpc.AnalyzerResult();
        grpc.Tokens.Add(new Grpc.AnalyzerToken { Token = "a", StartOffset = 0, EndOffset = 1, Position = 0 });
        grpc.Tokens.Add(new Grpc.AnalyzerToken { Token = "b", StartOffset = 2, EndOffset = 3, Position = 1 });

        AnalyzerResult result = AnalyzerResult.FromGrpc(grpc);

        Assert.Equal(2, result.Tokens.Count);
        Assert.Equal("a", result.Tokens[0].Token);
        Assert.Equal("b", result.Tokens[1].Token);
    }

    [Fact]
    public void Compact_maps_compaction_id()
    {
        var grpc = new Grpc.ManualCompactionResponse { CompactionID = 42 };

        CompactResp resp = CompactResp.FromGrpc(grpc);

        Assert.Equal(42, resp.CompactionId);
    }

    [Fact]
    public void DumpMessageInfo_maps_message_id_payload_and_properties()
    {
        var grpc = new Grpc.ImmutableMessage
        {
            Id = new Grpc.MessageID { Id = "msg-1", WALName = Grpc.WALName.RocksMq },
            Payload = ByteString.CopyFromUtf8("hello")
        };
        grpc.Properties["k"] = "v";

        DumpMessageInfo info = DumpMessageInfo.FromGrpc(grpc);

        Assert.NotNull(info.MessageId);
        Assert.Equal("msg-1", info.MessageId.Id);
        Assert.Equal(WalName.RocksMq, info.MessageId.WalName);
        Assert.Equal(Encoding.UTF8.GetBytes("hello"), info.Payload.ToArray());
        Assert.Equal("v", info.Properties["k"]);
    }

    [Fact]
    public void DumpMessageInfo_maps_null_message_id()
    {
        var grpc = new Grpc.ImmutableMessage();

        DumpMessageInfo info = DumpMessageInfo.FromGrpc(grpc);

        Assert.Null(info.MessageId);
        Assert.Empty(info.Payload.ToArray());
        Assert.Empty(info.Properties);
    }

    [Fact]
    public async Task DumpMessages_forwards_enumerated_messages()
    {
        var expected = new DumpMessageInfo(
            new MessageID("msg-1", WalName.RocksMq),
            new byte[] { 1, 2, 3 },
            new Dictionary<string, string> { ["k"] = "v" });

        var resp = new DumpMessagesResp(ct => YieldAsync(expected));

        var collected = new List<DumpMessageInfo>();
        await foreach (DumpMessageInfo item in resp)
        {
            collected.Add(item);
        }

        DumpMessageInfo single = Assert.Single(collected);
        Assert.Equal("msg-1", single.MessageId!.Id);
        Assert.Equal(new byte[] { 1, 2, 3 }, single.Payload.ToArray());
        Assert.Equal("v", single.Properties["k"]);

        static async IAsyncEnumerable<DumpMessageInfo> YieldAsync(DumpMessageInfo info)
        {
            await Task.Yield();
            yield return info;
        }
    }

    [Fact]
    public void FlushAll_maps_flush_all_timestamp()
    {
#pragma warning disable CS0612
        var grpc = new Grpc.FlushAllResponse { FlushAllTs = 123 };
        FlushAllResp resp = FlushAllResp.FromGrpc(grpc);
#pragma warning restore CS0612

        Assert.Equal(123UL, resp.FlushAllTimestamp);
    }

    [Fact]
    public void Flush_maps_coll_seg_ids()
    {
        var grpc = new Grpc.FlushResponse();
        var first = new Grpc.LongArray();
        first.Data.Add(11);
        first.Data.Add(12);
        grpc.CollSegIDs["c1"] = first;
        grpc.CollSegIDs["c2"] = new Grpc.LongArray();

        FlushResp resp = FlushResp.FromGrpc(grpc);

        Assert.Equal(2, resp.CollSegIDs.Count);
        Assert.Equal(new[] { 11L, 12L }, resp.CollSegIDs["c1"]);
        Assert.Empty(resp.CollSegIDs["c2"]);
    }

    [Fact]
    public void GetCompactionPlans_maps_state()
    {
        var grpc = new Grpc.GetCompactionPlansResponse { State = Grpc.CompactionState.Completed };

        GetCompactionPlansResp resp = GetCompactionPlansResp.FromGrpc(grpc);

        Assert.Equal(CompactionState.Completed, resp.State);
    }

    [Fact]
    public void GetCompactionState_maps_state()
    {
        var grpc = new Grpc.GetCompactionStateResponse
        {
            State = Grpc.CompactionState.Executing,
            ExecutingPlanNo = 1,
            TimeoutPlanNo = 2,
            CompletedPlanNo = 3,
            FailedPlanNo = 4
        };

        GetCompactionStateResp resp = GetCompactionStateResp.FromGrpc(grpc);

        Assert.Equal(CompactionState.Executing, resp.State);
        Assert.Equal(1, resp.ExecutingPlanNo);
        Assert.Equal(2, resp.TimeoutPlanNo);
        Assert.Equal(3, resp.CompletedPlanNo);
        Assert.Equal(4, resp.FailedPlanNo);
    }

    [Fact]
    public void GetFlushAllState_maps_flushed()
    {
        var grpc = new Grpc.GetFlushAllStateResponse { Flushed = true };
        GetFlushAllStateResp resp = GetFlushAllStateResp.FromGrpc(grpc);

        Assert.True(resp.Flushed);
    }

    [Fact]
    public void GetFlushState_maps_flushed()
    {
        var grpc = new Grpc.GetFlushStateResponse { Flushed = true };
        GetFlushStateResp resp = GetFlushStateResp.FromGrpc(grpc);

        Assert.True(resp.Flushed);

        var notFlushed = GetFlushStateResp.FromGrpc(new Grpc.GetFlushStateResponse { Flushed = false });
        Assert.False(notFlushed.Flushed);
    }

    [Fact]
    public void GetMetrics_maps_response_and_component_name()
    {
        var grpc = new Grpc.GetMetricsResponse { Response = "json", ComponentName = "rootcoord" };

        GetMetricsResp resp = GetMetricsResp.FromGrpc(grpc);

        Assert.Equal("json", resp.Response);
        Assert.Equal("rootcoord", resp.ComponentName);
    }

    [Fact]
    public void GetPersistentSegmentInfo_maps_infos()
    {
        var grpc = new Grpc.GetPersistentSegmentInfoResponse();
        grpc.Infos.Add(new Grpc.PersistentSegmentInfo { SegmentID = 1, CollectionID = 2, NumRows = 100 });

        GetPersistentSegmentInfoResp resp = GetPersistentSegmentInfoResp.FromGrpc(grpc);

        PersistentSegmentInfo info = Assert.Single(resp.Infos);
        Assert.Equal(1, info.SegmentId);
        Assert.Equal(2, info.CollectionId);
        Assert.Equal(100, info.NumRows);
    }

    [Fact]
    public void GetQuerySegmentInfo_maps_infos()
    {
        var grpc = new Grpc.GetQuerySegmentInfoResponse();
        grpc.Infos.Add(new Grpc.QuerySegmentInfo
        {
            SegmentID = 1,
            CollectionID = 2,
            PartitionID = 3,
            MemSize = 1024,
            NumRows = 50,
            IndexName = "idx"
        });

        GetQuerySegmentInfoResp resp = GetQuerySegmentInfoResp.FromGrpc(grpc);

        QuerySegmentInfo info = Assert.Single(resp.Infos);
        Assert.Equal(1, info.SegmentId);
        Assert.Equal(2, info.CollectionId);
        Assert.Equal(3, info.PartitionId);
        Assert.Equal(1024, info.MemSize);
        Assert.Equal(50, info.NumRows);
        Assert.Equal("idx", info.IndexName);
    }

    [Fact]
    public void GetReplicateConfiguration_maps_configuration()
    {
        var grpc = new Grpc.GetReplicateConfigurationResponse
        {
            Configuration = new Grpc.ReplicateConfiguration()
        };
        grpc.Configuration.Clusters.Add(new Grpc.MilvusCluster
        {
            ClusterId = "cluster-a",
            ConnectionParam = new Grpc.ConnectionParam { Uri = "http://localhost:19530", Token = "tok" },
            Pchannels = { "ch1" }
        });
        grpc.Configuration.CrossClusterTopology.Add(new Grpc.CrossClusterTopology
        {
            SourceClusterId = "a",
            TargetClusterId = "b"
        });

        GetReplicateConfigurationResp resp = GetReplicateConfigurationResp.FromGrpc(grpc);

        MilvusCluster cluster = Assert.Single(resp.Configuration.Clusters);
        Assert.Equal("cluster-a", cluster.ClusterId);
        Assert.Equal("http://localhost:19530", cluster.Uri);
        Assert.Equal("tok", cluster.Token);
        Assert.Equal(new[] { "ch1" }, cluster.Pchannels);

        CrossClusterTopology topology = Assert.Single(resp.Configuration.CrossClusterTopologies);
        Assert.Equal("a", topology.SourceClusterId);
        Assert.Equal("b", topology.TargetClusterId);
    }

    [Fact]
    public void GetReplicateConfiguration_maps_null_to_empty_configuration()
    {
        var grpc = new Grpc.GetReplicateConfigurationResponse();

        GetReplicateConfigurationResp resp = GetReplicateConfigurationResp.FromGrpc(grpc);

        Assert.NotNull(resp.Configuration);
        Assert.Empty(resp.Configuration.Clusters);
        Assert.Empty(resp.Configuration.CrossClusterTopologies);
    }

    [Fact]
    public void GetReplicateInfo_maps_checkpoint()
    {
        var grpc = new Grpc.GetReplicateInfoResponse
        {
            Checkpoint = new Grpc.ReplicateCheckpoint
            {
                ClusterId = "cluster-a",
                Pchannel = "by-dev-rootcoord-dml_0",
                MessageId = new Grpc.MessageID { Id = "msg", WALName = Grpc.WALName.RocksMq },
                TimeTick = 42
            },
            SalvageCheckpoint = new Grpc.ReplicateCheckpoint
            {
                ClusterId = "cluster-a",
                Pchannel = "by-dev-rootcoord-dml_0",
                MessageId = new Grpc.MessageID { Id = "salvage", WALName = Grpc.WALName.Kafka },
                TimeTick = 7
            }
        };

        GetReplicateInfoResp resp = GetReplicateInfoResp.FromGrpc(grpc);

        Assert.NotNull(resp.Checkpoint);
        Assert.Equal("cluster-a", resp.Checkpoint.ClusterId);
        Assert.Equal("by-dev-rootcoord-dml_0", resp.Checkpoint.Pchannel);
        Assert.Equal("msg", resp.Checkpoint.MessageId);
        Assert.Equal(42UL, resp.Checkpoint.TimeTick);

        Assert.NotNull(resp.SalvageCheckpoint);
        Assert.Equal("salvage", resp.SalvageCheckpoint.MessageId);
        Assert.Equal(7UL, resp.SalvageCheckpoint.TimeTick);
    }

    [Fact]
    public void GetReplicateInfo_maps_null_checkpoint()
    {
        var grpc = new Grpc.GetReplicateInfoResponse();

        GetReplicateInfoResp resp = GetReplicateInfoResp.FromGrpc(grpc);

        Assert.Null(resp.Checkpoint);
    }

    [Fact]
    public void GetServerVersion_maps_version_from_get_version_response()
    {
        var grpc = new Grpc.GetVersionResponse { Version = "v2.5.0" };

        GetServerVersionResp resp = GetServerVersionResp.FromGrpc(grpc);

        Assert.Equal("v2.5.0", resp.Version);
        Assert.Null(resp.BuildTime);
        Assert.Null(resp.GitCommit);
        Assert.Null(resp.GoVersion);
        Assert.Null(resp.DeployMode);
    }

    [Fact]
    public void GetServerVersion_maps_server_info_from_connect_response()
    {
        var grpc = new Grpc.ConnectResponse
        {
            ServerInfo = new Grpc.ServerInfo
            {
                BuildTags = "v2.5.0",
                BuildTime = "2026-01-01",
                GitCommit = "abc123",
                GoVersion = "go1.23",
                DeployMode = "standalone"
            }
        };

        GetServerVersionResp resp = GetServerVersionResp.FromGrpc(grpc);

        Assert.Equal("v2.5.0", resp.Version);
        Assert.Equal("2026-01-01", resp.BuildTime);
        Assert.Equal("abc123", resp.GitCommit);
        Assert.Equal("go1.23", resp.GoVersion);
        Assert.Equal("standalone", resp.DeployMode);
    }

    [Fact]
    public void GetServerVersion_handles_null_server_info()
    {
        var grpc = new Grpc.ConnectResponse();

        GetServerVersionResp resp = GetServerVersionResp.FromGrpc(grpc);

        Assert.Equal("", resp.Version);
        Assert.Null(resp.BuildTime);
        Assert.Null(resp.GitCommit);
        Assert.Null(resp.GoVersion);
        Assert.Null(resp.DeployMode);
    }

    [Fact]
    public void Optimize_maps_compaction_id_and_target_size()
    {
        var resp = new OptimizeResp("success", "book", 123, "512", ["waiting for indexes", "compacting"]);

        Assert.Equal("success", resp.Status);
        Assert.Equal("book", resp.CollectionName);
        Assert.Equal(123, resp.CompactionId);
        Assert.Equal("512", resp.TargetSize);
        Assert.Equal(new[] { "waiting for indexes", "compacting" }, resp.Progress);
    }

    [Fact]
    public void Optimize_maps_null_compaction_id()
    {
        var resp = new OptimizeResp("success", "book", null, null, ["waiting for indexes"]);

        Assert.Equal("success", resp.Status);
        Assert.Equal("book", resp.CollectionName);
        Assert.Null(resp.CompactionId);
        Assert.Null(resp.TargetSize);
        Assert.Equal(["waiting for indexes"], resp.Progress);
    }

    [Fact]
    public void PersistentSegmentInfo_maps_segment_fields()
    {
        var grpc = new Grpc.PersistentSegmentInfo
        {
            SegmentID = 1,
            CollectionID = 2,
            PartitionID = 3,
            NumRows = 100,
            State = Grpc.SegmentState.Flushed,
            Level = Grpc.SegmentLevel.L0,
            IsSorted = true,
            StorageVersion = 7
        };

        PersistentSegmentInfo info = PersistentSegmentInfo.FromGrpc(grpc);

        Assert.Equal(1, info.SegmentId);
        Assert.Equal(2, info.CollectionId);
        Assert.Equal(3, info.PartitionId);
        Assert.Equal(100, info.NumRows);
        Assert.Equal(SegmentState.Flushed, info.State);
        Assert.Equal(SegmentLevel.L0, info.Level);
        Assert.True(info.IsSorted);
        Assert.Equal(7, info.StorageVersion);
    }

    [Fact]
    public void QuerySegmentInfo_maps_segment_fields()
    {
        var grpc = new Grpc.QuerySegmentInfo
        {
            SegmentID = 1,
            CollectionID = 2,
            PartitionID = 3,
            MemSize = 1024,
            NumRows = 50,
            IndexName = "idx",
            IndexID = 9,
            State = Grpc.SegmentState.Sealed,
            Level = Grpc.SegmentLevel.L1,
            IsSorted = true,
            StorageVersion = 3
        };

        QuerySegmentInfo info = QuerySegmentInfo.FromGrpc(grpc);

        Assert.Equal(1, info.SegmentId);
        Assert.Equal(2, info.CollectionId);
        Assert.Equal(3, info.PartitionId);
        Assert.Equal(1024, info.MemSize);
        Assert.Equal(50, info.NumRows);
        Assert.Equal("idx", info.IndexName);
        Assert.Equal(9, info.IndexId);
        Assert.Equal(SegmentState.Sealed, info.State);
        Assert.Equal(SegmentLevel.L1, info.Level);
        Assert.True(info.IsSorted);
        Assert.Equal(3, info.StorageVersion);
    }

    [Fact]
    public void RunAnalyzer_maps_results()
    {
        var grpc = new Grpc.RunAnalyzerResponse();
        var result = new Grpc.AnalyzerResult();
        result.Tokens.Add(new Grpc.AnalyzerToken { Token = "milvus", StartOffset = 0, EndOffset = 6, Position = 0 });
        grpc.Results.Add(result);

        RunAnalyzerResp resp = RunAnalyzerResp.FromGrpc(grpc);

        AnalyzerResult single = Assert.Single(resp.Results);
        AnalyzerToken token = Assert.Single(single.Tokens);
        Assert.Equal("milvus", token.Token);
        Assert.Equal(0, token.StartOffset);
        Assert.Equal(6, token.EndOffset);
        Assert.Equal(0, token.Position);
    }

    [Fact]
    public void UpdateReplicateConfiguration_builds_empty_response()
    {
        UpdateReplicateConfigurationResp resp = UpdateReplicateConfigurationResp.FromGrpc();
        Assert.NotNull(resp);
    }
}
