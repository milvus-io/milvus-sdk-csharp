using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Types;

[Trait("Category", "Unit")]
public class TypesModelsTests
{
    [Fact]
    public void FunctionSchema_constructor_sets_all_properties()
    {
        var schema = new FunctionSchema(
            "bm25_fn", FunctionType.Bm25, new[] { "text" }, new[] { "sparse" }, "Full-text search");

        Assert.Equal(0, schema.Id); // never assigned, defaults to 0
        Assert.Equal("bm25_fn", schema.Name);
        Assert.Equal(FunctionType.Bm25, schema.Type);
        Assert.Equal("Full-text search", schema.Description);
        Assert.Equal(new[] { "text" }, schema.InputFieldNames);
        Assert.Equal(new[] { "sparse" }, schema.OutputFieldNames);
    }

    [Fact]
    public void FunctionSchema_default_description_is_empty_string()
    {
        var schema = new FunctionSchema("embed", FunctionType.TextEmbedding, new[] { "in" }, new[] { "out" });

        Assert.Equal("", schema.Description);
    }

    [Fact]
    public void FunctionSchema_CreateBm25_builds_bm25_schema()
    {
        var schema = FunctionSchema.CreateBm25("bm25", "input", "output", "desc");

        Assert.Equal("bm25", schema.Name);
        Assert.Equal(FunctionType.Bm25, schema.Type);
        Assert.Equal("desc", schema.Description);
        Assert.Equal(new[] { "input" }, schema.InputFieldNames);
        Assert.Equal(new[] { "output" }, schema.OutputFieldNames);
    }

    [Fact]
    public void FunctionSchema_rejects_null_or_blank_name()
    {
        Assert.Throws<ArgumentNullException>(() => new FunctionSchema(null!, FunctionType.Bm25, new[] { "i" }, new[] { "o" }));
        Assert.Throws<ArgumentException>(() => new FunctionSchema(" ", FunctionType.Bm25, new[] { "i" }, new[] { "o" }));
    }

    [Fact]
    public void FunctionSchema_rejects_null_field_name_lists()
    {
        Assert.Throws<ArgumentNullException>(() => new FunctionSchema("f", FunctionType.Bm25, null!, new[] { "o" }));
        Assert.Throws<ArgumentNullException>(() => new FunctionSchema("f", FunctionType.Bm25, new[] { "i" }, null!));
    }

    [Fact]
    public void CreateVarchar_validates_max_length()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FieldSchema.CreateVarchar("title", maxLength: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => FieldSchema.CreateVarchar("title", maxLength: -1));
    }

    [Fact]
    public void CreateFloatVector_validates_dimension()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FieldSchema.CreateFloatVector("embedding", dimension: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => FieldSchema.CreateFloatVector("embedding", dimension: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => FieldSchema.CreateFloatVector("embedding", dimension: -4));
    }

    [Fact]
    public void FunctionSchema_roundtrips_server_assigned_id()
    {
        var grpc = new Grpc.FunctionSchema
        {
            Name = "bm25_fn",
            Id = 42,
            Type = Grpc.FunctionType.Bm25,
            Description = "desc"
        };
        grpc.InputFieldNames.Add("text");
        grpc.OutputFieldNames.Add("sparse");

        FunctionSchema schema = FunctionSchema.FromGrpc(grpc);

        Assert.Equal(42, schema.Id);
        Assert.Equal("bm25_fn", schema.Name);
        Assert.Equal(FunctionType.Bm25, schema.Type);

        Grpc.FunctionSchema back = schema.ToGrpcFunctionSchema();

        Assert.Equal(42, back.Id);
        Assert.Equal(Grpc.FunctionType.Bm25, back.Type);
        Assert.Equal(new[] { "text" }, back.InputFieldNames);
        Assert.Equal(new[] { "sparse" }, back.OutputFieldNames);
    }

    [Fact]
    public void FunctionSchema_ToGrpc_maps_all_members()
    {
        var schema = new FunctionSchema(
            "bm25_fn", FunctionType.Bm25, new[] { "a", "b" }, new[] { "c" }, "desc");

        Grpc.FunctionSchema grpc = schema.ToGrpcFunctionSchema();

        Assert.Equal("bm25_fn", grpc.Name);
        Assert.Equal(Grpc.FunctionType.Bm25, grpc.Type);
        Assert.Equal("desc", grpc.Description);
        Assert.Equal(new[] { "a", "b" }, grpc.InputFieldNames);
        Assert.Equal(new[] { "c" }, grpc.OutputFieldNames);
    }

    [Fact]
    public void MilvusHealthState_constructor_sets_properties()
    {
        var state = new MilvusHealthState(true, "healthy", MilvusErrorCode.Success);

        Assert.True(state.IsHealthy);
        Assert.Equal("healthy", state.Reason);
        Assert.Equal(MilvusErrorCode.Success, state.ErrorCode);
    }

    [Fact]
    public void MilvusHealthState_implements_record_equality()
    {
        var a = new MilvusHealthState(false, "unhealthy", MilvusErrorCode.UnexpectedError);
        var b = new MilvusHealthState(false, "unhealthy", MilvusErrorCode.UnexpectedError);
        var c = new MilvusHealthState(true, "unhealthy", MilvusErrorCode.UnexpectedError);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void FromGrpc_long_ids_populates_long_ids()
    {
        var grpc = new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1, 2, 3 } } };

        MilvusIds ids = MilvusIds.FromGrpc(grpc);

        IReadOnlyList<long>? longIds = ids.LongIds;
        Assert.NotNull(longIds);
        Assert.Equal(new long[] { 1, 2, 3 }, longIds);
        Assert.Null(ids.StringIds);
    }

    [Fact]
    public void FromGrpc_string_ids_populates_string_ids()
    {
        var grpc = new Grpc.IDs { StrId = new Grpc.StringArray { Data = { "a", "b" } } };

        MilvusIds ids = MilvusIds.FromGrpc(grpc);

        IReadOnlyList<string>? stringIds = ids.StringIds;
        Assert.NotNull(stringIds);
        Assert.Equal(new[] { "a", "b" }, stringIds);
        Assert.Null(ids.LongIds);
    }

    [Fact]
    public void FromGrpc_none_id_field_yields_default()
    {
        MilvusIds ids = MilvusIds.FromGrpc(new Grpc.IDs());

        Assert.Null(ids.LongIds);
        Assert.Null(ids.StringIds);
    }

    [Fact]
    public void MilvusIds_equality_compares_long_ids_by_content()
    {
        var a = MilvusIds.FromGrpc(new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1, 2 } } });
        var b = MilvusIds.FromGrpc(new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1, 2 } } });
        var c = MilvusIds.FromGrpc(new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1, 3 } } });

        Assert.True(a == b);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());

        Assert.True(a != c);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void MilvusIds_long_and_string_ids_are_never_equal()
    {
        var longs = MilvusIds.FromGrpc(new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1 } } });
        var strings = MilvusIds.FromGrpc(new Grpc.IDs { StrId = new Grpc.StringArray { Data = { "1" } } });

        Assert.NotEqual(longs, strings);
        Assert.False(longs == strings);
        Assert.True(longs != strings);
    }

    [Fact]
    public void MilvusIds_default_instances_are_equal()
    {
        MilvusIds a = default;
        MilvusIds b = MilvusIds.FromGrpc(new Grpc.IDs());

        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ReplicaInfo_FromGrpc_maps_all_properties()
    {
        var grpc = new Grpc.ReplicaInfo
        {
            ReplicaID = 100,
            CollectionID = 42,
            ResourceGroupName = "rg1"
        };
        grpc.PartitionIds.AddRange(new long[] { 1, 2 });
        grpc.NodeIds.AddRange(new long[] { 5, 6 });
        grpc.NumOutboundNode.Add("rg2", 3);
        grpc.ShardReplicas.Add(new Grpc.ShardReplica
        {
            LeaderID = 10,
            LeaderAddr = "host:port",
            DmChannelName = "ch-0"
        });

        ReplicaInfo info = ReplicaInfo.FromGrpc(grpc);

        Assert.Equal(100, info.ReplicaId);
        Assert.Equal(42, info.CollectionId);
        Assert.Equal(new long[] { 1, 2 }, info.PartitionIds);
        Assert.Equal(new long[] { 5, 6 }, info.NodeIds);
        Assert.Equal("rg1", info.ResourceGroupName);
        Assert.Equal(3, info.NumOutboundNode["rg2"]);

        ShardReplica shard = Assert.IsType<ShardReplica>(Assert.Single(info.ShardReplicas));
        Assert.Equal(10, shard.LeaderId);
        Assert.Equal("host:port", shard.LeaderAddress);
        Assert.Equal("ch-0", shard.DmChannelName);
    }

    [Fact]
    public void ReplicateCheckpoint_FromGrpc_maps_all_properties()
    {
        var grpc = new Grpc.ReplicateCheckpoint
        {
            ClusterId = "cluster-1",
            Pchannel = "by-dev-rootcoord-dml_0",
            TimeTick = 42
        };
        grpc.MessageId = new Grpc.MessageID { Id = "msg-123", WALName = Grpc.WALName.RocksMq };

        ReplicateCheckpoint checkpoint = ReplicateCheckpoint.FromGrpc(grpc);

        Assert.Equal("cluster-1", checkpoint.ClusterId);
        Assert.Equal("by-dev-rootcoord-dml_0", checkpoint.Pchannel);
        Assert.Equal("msg-123", checkpoint.MessageId);
        Assert.Equal(WalName.RocksMq, checkpoint.WalName);
        Assert.Equal(42UL, checkpoint.TimeTick);
    }

    [Fact]
    public void ReplicateCheckpoint_missing_message_id_yields_empty_string()
    {
        var grpc = new Grpc.ReplicateCheckpoint { ClusterId = "c", Pchannel = "p", TimeTick = 0 };

        ReplicateCheckpoint checkpoint = ReplicateCheckpoint.FromGrpc(grpc);

        Assert.Equal("", checkpoint.MessageId);
        Assert.Equal(0UL, checkpoint.TimeTick);
    }

    [Fact]
    public void ShardReplica_FromGrpc_maps_all_properties()
    {
        var grpc = new Grpc.ShardReplica
        {
            LeaderID = 10,
            LeaderAddr = "10.0.0.1:19530",
            DmChannelName = "by-dev-rootcoord-dml_0"
        };
        grpc.NodeIds.AddRange(new long[] { 10, 11 });

        ShardReplica replica = ShardReplica.FromGrpc(grpc);

        Assert.Equal(10, replica.LeaderId);
        Assert.Equal("10.0.0.1:19530", replica.LeaderAddress);
        Assert.Equal("by-dev-rootcoord-dml_0", replica.DmChannelName);
        Assert.Equal(new long[] { 10, 11 }, replica.NodeIds);
    }

    [Fact]
    public void MilvusIds_equality_and_hash_code()
    {
        MilvusIds a = MilvusIds.FromGrpc(new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1, 2, 3 } } });
        MilvusIds b = MilvusIds.FromGrpc(new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1, 2, 3 } } });
        MilvusIds different = MilvusIds.FromGrpc(new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1, 2 } } });
        MilvusIds strings = MilvusIds.FromGrpc(new Grpc.IDs { StrId = new Grpc.StringArray { Data = { "a" } } });

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a.Equals(different));
        Assert.False(a.Equals(strings));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
