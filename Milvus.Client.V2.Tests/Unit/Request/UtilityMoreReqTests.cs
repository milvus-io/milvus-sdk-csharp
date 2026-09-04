using Xunit;

using Milvus.Client.V2.Requests.Utility;

namespace Milvus.Client.V2.Tests.Unit.Request;

[Trait("Category", "Unit")]
public class UtilityMoreReqTests
{
    [Fact]
    public void Compact_maps_collection_name_major_and_collection_id()
    {
        var request = new CompactReq
        {
            CollectionName = "coll",
            IsMajorCompaction = true,
            IsL0Compaction = true
        };

        Grpc.ManualCompactionRequest grpc = request.ToGrpcManualCompactionRequest(42);

        Assert.Equal(42, grpc.CollectionID);
        Assert.Equal("coll", grpc.CollectionName);
        Assert.True(grpc.MajorCompaction);
        Assert.True(grpc.L0Compaction);
    }

    [Fact]
    public void Compact_sets_target_size_when_specified()
    {
        var request = new CompactReq { CollectionName = "coll", TargetSize = 512 };

        Grpc.ManualCompactionRequest grpc = request.ToGrpcManualCompactionRequest(1);

        Assert.Equal(512, grpc.TargetSize);
    }

    [Fact]
    public void Compact_converts_target_size_unit_to_mb()
    {
        var request = new CompactReq { CollectionName = "coll", TargetSize = 1, TargetSizeUnit = "gb" };

        Grpc.ManualCompactionRequest grpc = request.ToGrpcManualCompactionRequest(1);

        Assert.Equal(1024, grpc.TargetSize);
    }

    [Fact]
    public void Compact_throws_when_target_size_not_positive()
    {
        var request = new CompactReq { CollectionName = "coll", TargetSize = 0 };

        Assert.Throws<ArgumentException>(() => request.ToGrpcManualCompactionRequest(1));
    }

    [Fact]
    public void Compact_throws_when_target_size_unit_invalid()
    {
        var request = new CompactReq { CollectionName = "coll", TargetSize = 512, TargetSizeUnit = "xb" };

        Assert.Throws<ArgumentException>(() => request.ToGrpcManualCompactionRequest(1));
    }

    [Fact]
    public void Compact_throws_when_target_size_overflows_decimal()
    {
        // A large target size in the tb/pb units overflows decimal during scaling; it must fail with the
        // clear ArgumentException, not an OverflowException.
        var request = new CompactReq { CollectionName = "coll", TargetSize = long.MaxValue, TargetSizeUnit = "pb" };

        ArgumentException ex = Assert.Throws<ArgumentException>(() => request.ToGrpcManualCompactionRequest(1));
        Assert.Contains("target size too large", ex.Message);
    }

    [Fact]
    public void Compact_leaves_target_size_zero_when_not_specified()
    {
        var request = new CompactReq { CollectionName = "coll" };

        Grpc.ManualCompactionRequest grpc = request.ToGrpcManualCompactionRequest(1);

        Assert.Equal(0, grpc.TargetSize);
    }

    [Fact]
    public void Compact_throws_when_collection_name_blank()
    {
        var request = new CompactReq { CollectionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcManualCompactionRequest(1));
    }

    [Fact]
    public void FlushAll_builds_empty_request()
    {
        Grpc.FlushAllRequest grpc = new FlushAllReq().ToGrpcFlushAllRequest();
        Assert.NotNull(grpc);
    }

    [Fact]
    public void Flush_maps_collection_names()
    {
        var request = new FlushReq { CollectionNames = new[] { "c1", "c2" } };

        Grpc.FlushRequest grpc = request.ToGrpcFlushRequest();

        Assert.Equal(new[] { "c1", "c2" }, grpc.CollectionNames);
    }

    [Fact]
    public void Flush_throws_when_collection_names_empty()
    {
        var request = new FlushReq { CollectionNames = Array.Empty<string>() };
        Assert.Throws<ArgumentException>(() => request.ToGrpcFlushRequest());
    }

    [Fact]
    public void Flush_throws_when_collection_names_null()
    {
        var request = new FlushReq { CollectionNames = null! };
        Assert.Throws<ArgumentNullException>(() => request.ToGrpcFlushRequest());
    }

    [Fact]
    public void GetFlushState_maps_segments_and_collection()
    {
        var request = new GetFlushStateReq { CollectionName = "coll", SegmentIds = new long[] { 1, 2, 3 } };

        Grpc.GetFlushStateRequest grpc = request.ToGrpcGetFlushStateRequest();

        Assert.Equal("coll", grpc.CollectionName);
        Assert.Equal(new long[] { 1, 2, 3 }, grpc.SegmentIDs);
    }

    [Fact]
    public void GetFlushState_validates_inputs()
    {
        Assert.Throws<ArgumentException>(() => new GetFlushStateReq { CollectionName = "", SegmentIds = new long[] { 1 } }.ToGrpcGetFlushStateRequest());
        Assert.Throws<ArgumentException>(() => new GetFlushStateReq { CollectionName = "coll", SegmentIds = [] }.ToGrpcGetFlushStateRequest());
    }

    [Fact]
    public void GetCompactionPlans_maps_compaction_id()
    {
        var request = new GetCompactionPlansReq { CompactionId = 77 };

        Grpc.GetCompactionPlansRequest grpc = request.ToGrpcGetCompactionPlansRequest();

        Assert.Equal(77, grpc.CompactionID);
    }

    [Fact]
    public void GetCompactionState_maps_compaction_id()
    {
        var request = new GetCompactionStateReq { CompactionId = 88 };

        Grpc.GetCompactionStateRequest grpc = request.ToGrpcGetCompactionStateRequest();

        Assert.Equal(88, grpc.CompactionID);
    }

    [Fact]
    public void GetFlushAllState_maps_flush_all_timestamp()
    {
        var request = new GetFlushAllStateReq { FlushAllTimestamp = 123 };

#pragma warning disable CS0612
        Grpc.GetFlushAllStateRequest grpc = request.ToGrpcGetFlushAllStateRequest();
        Assert.Equal(123UL, grpc.FlushAllTs);
#pragma warning restore CS0612
    }

    [Fact]
    public void GetMetrics_maps_request()
    {
        var request = new GetMetricsReq { Request = "{\"key\":\"value\"}" };

        Grpc.GetMetricsRequest grpc = request.ToGrpcGetMetricsRequest();

        Assert.Equal("{\"key\":\"value\"}", grpc.Request);
    }

    [Fact]
    public void GetMetrics_throws_when_request_blank()
    {
        var request = new GetMetricsReq { Request = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcGetMetricsRequest());
    }

    [Fact]
    public void GetPersistentSegmentInfo_maps_collection_name()
    {
        var request = new GetPersistentSegmentInfoReq { DatabaseName = "db1", CollectionName = "coll" };

        Grpc.GetPersistentSegmentInfoRequest grpc = request.ToGrpcGetPersistentSegmentInfoRequest();

        Assert.Equal("db1", grpc.DbName);
        Assert.Equal("coll", grpc.CollectionName);
    }

    [Fact]
    public void GetPersistentSegmentInfo_throws_when_collection_name_blank()
    {
        var request = new GetPersistentSegmentInfoReq { CollectionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcGetPersistentSegmentInfoRequest());
    }

    [Fact]
    public void GetQuerySegmentInfo_maps_collection_name()
    {
        var request = new GetQuerySegmentInfoReq { DatabaseName = "db1", CollectionName = "coll" };

        Grpc.GetQuerySegmentInfoRequest grpc = request.ToGrpcGetQuerySegmentInfoRequest();

        Assert.Equal("db1", grpc.DbName);
        Assert.Equal("coll", grpc.CollectionName);
    }

    [Fact]
    public void GetQuerySegmentInfo_throws_when_collection_name_blank()
    {
        var request = new GetQuerySegmentInfoReq { CollectionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcGetQuerySegmentInfoRequest());
    }

    [Fact]
    public void GetServerVersion_defaults_to_non_detail()
    {
        var request = new GetServerVersionReq();
        Assert.False(request.Detail);
    }

    [Fact]
    public void GetServerVersion_allows_detail()
    {
        var request = new GetServerVersionReq { Detail = true };
        Assert.True(request.Detail);
    }
}
