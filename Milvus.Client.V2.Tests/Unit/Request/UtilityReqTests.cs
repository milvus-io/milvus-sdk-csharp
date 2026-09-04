using Xunit;

using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Request;

[Trait("Category", "Unit")]
public class UtilityReqTests
{
    [Fact]
    public void GetReplicateInfo_maps_source_cluster_and_pchannel()
    {
        var request = new GetReplicateInfoReq
        {
            SourceClusterId = "cluster-a",
            TargetPchannel = "by-dev-rootcoord-dml_0"
        };

        Grpc.GetReplicateInfoRequest grpc = request.ToGrpcRequest();

        Assert.Equal("cluster-a", grpc.SourceClusterId);
        Assert.Equal("by-dev-rootcoord-dml_0", grpc.TargetPchannel);
    }

    [Fact]
    public void UpdateReplicateConfiguration_maps_clusters_and_topology()
    {
        var request = new UpdateReplicateConfigurationReq
        {
            ReplicateConfiguration = new ReplicateConfiguration(
                new[]
                {
                    new MilvusCluster("cluster-a", "http://localhost:19530", "token-a", new[] { "ch1" }),
                    new MilvusCluster("cluster-b", "http://localhost:19531", "token-b")
                },
                new[] { new CrossClusterTopology("cluster-a", "cluster-b") })
        };

        Grpc.UpdateReplicateConfigurationRequest grpc = request.ToGrpcRequest();
        Grpc.ReplicateConfiguration config = grpc.ReplicateConfiguration;

        Assert.Equal(2, config.Clusters.Count);
        Assert.Equal("cluster-a", config.Clusters[0].ClusterId);
        Assert.Equal("http://localhost:19530", config.Clusters[0].ConnectionParam.Uri);
        Assert.Equal("token-a", config.Clusters[0].ConnectionParam.Token);
        Assert.Equal(new[] { "ch1" }, config.Clusters[0].Pchannels);
        Assert.Equal("cluster-b", config.Clusters[1].ClusterId);
        Assert.Single(config.CrossClusterTopology);
        Assert.Equal("cluster-a", config.CrossClusterTopology[0].SourceClusterId);
        Assert.Equal("cluster-b", config.CrossClusterTopology[0].TargetClusterId);
    }

    [Fact]
    public void DumpMessages_maps_pchannel_and_start_message()
    {
        var request = new DumpMessagesReq
        {
            Pchannel = "by-dev-rootcoord-dml_0",
            StartMessageId = new MessageID("0001020304050607", WalName.RocksMq),
            StartTimetick = 100,
            EndTimetick = 200,
            IncludeStartMessage = true
        };

        Grpc.DumpMessagesRequest grpc = request.ToGrpcRequest();

        Assert.Equal("by-dev-rootcoord-dml_0", grpc.Pchannel);
        Assert.Equal("0001020304050607", grpc.StartMessageId.Id);
        Assert.Equal(Grpc.WALName.RocksMq, grpc.StartMessageId.WALName);
        Assert.Equal(100UL, grpc.StartTimetick);
        Assert.Equal(200UL, grpc.EndTimetick);
        Assert.True(grpc.IncludeStartMessage);
    }

    [Fact]
    public void DumpMessages_throws_when_start_message_missing()
    {
        var request = new DumpMessagesReq { Pchannel = "ch" };
        Assert.Throws<ArgumentNullException>(() => request.ToGrpcRequest());
    }

    [Fact]
    public void UseDatabase_throws_when_database_blank()
    {
        var request = new UseDatabaseReq { DatabaseName = " " };
        Assert.Throws<ArgumentException>(() => request.Validate());
    }

    [Fact]
    public void GetReplicateConfiguration_builds_empty_request()
    {
        Grpc.GetReplicateConfigurationRequest grpc = GetReplicateConfigurationReq.ToGrpcRequest();
        Assert.NotNull(grpc);
    }

    [Fact]
    public void Optimize_throws_when_collection_blank()
    {
        var request = new OptimizeReq { CollectionName = " " };
        Assert.Throws<ArgumentException>(() => request.Validate());
    }

    [Fact]
    public void RunAnalyzer_serializes_analyzer_params_as_json()
    {
        var request = new RunAnalyzerReq
        {
            AnalyzerParams = new Dictionary<string, object> { ["type"] = "english", ["enable_position"] = true },
            Texts = new[] { "Hello Milvus!" },
            WithDetail = true,
            WithHash = false,
            CollectionName = "coll",
            FieldName = "text",
            AnalyzerNames = new[] { "standard" }
        };

        Grpc.RunAnalyzerRequest grpc = request.ToGrpcRunAnalyzerRequest();

        Assert.Equal("{\"type\":\"english\",\"enable_position\":true}", grpc.AnalyzerParams);
        Assert.Equal(new[] { "Hello Milvus!" }, grpc.Placeholder.Select(b => b.ToStringUtf8()));
        Assert.True(grpc.WithDetail);
        Assert.False(grpc.WithHash);
        Assert.Equal("coll", grpc.CollectionName);
        Assert.Equal("text", grpc.FieldName);
        Assert.Equal(new[] { "standard" }, grpc.AnalyzerNames);
    }

    [Fact]
    public void RunAnalyzer_throws_when_texts_empty()
    {
        var request = new RunAnalyzerReq { Texts = Array.Empty<string>() };
        Assert.Throws<ArgumentException>(() => request.ToGrpcRunAnalyzerRequest());
    }
}
