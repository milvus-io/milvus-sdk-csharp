using Xunit;

using Milvus.Client.V2.Requests.Collection;

namespace Milvus.Client.V2.Tests.Unit.Request;

[Trait("Category", "Unit")]
public class CollectionPropertiesReqTests
{
    [Fact]
    public void AlterCollectionProperties_maps_properties_and_delete_keys()
    {
        var request = new AlterCollectionPropertiesReq
        {
            CollectionName = "book",
        };
        request.Properties["ttl.seconds"] = "3600";
        request.Properties["partitionkey.isolation"] = "true";
        request.DeleteKeys = new[] { "stale_key" };

        Grpc.AlterCollectionRequest grpc = request.ToGrpcRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal(2, grpc.Properties.Count);
        Assert.Equal("3600", grpc.Properties.Single(p => p.Key == "ttl.seconds").Value);
        Assert.Equal("true", grpc.Properties.Single(p => p.Key == "partitionkey.isolation").Value);
        Assert.Equal(new[] { "stale_key" }, grpc.DeleteKeys);
    }

    [Fact]
    public void AlterCollectionProperties_throws_when_name_blank()
    {
        Assert.Throws<ArgumentException>(() => new AlterCollectionPropertiesReq().ToGrpcRequest());
    }

    [Fact]
    public void DropCollectionProperties_maps_delete_keys()
    {
        var request = new DropCollectionPropertiesReq
        {
            CollectionName = "book",
            DeleteKeys = new[] { "ttl.seconds", "partitionkey.isolation" }
        };

        Grpc.AlterCollectionRequest grpc = request.ToGrpcRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Empty(grpc.Properties);
        Assert.Equal(new[] { "ttl.seconds", "partitionkey.isolation" }, grpc.DeleteKeys);
    }

    [Fact]
    public void DropCollectionProperties_throws_when_delete_keys_empty()
    {
        var request = new DropCollectionPropertiesReq { CollectionName = "book", DeleteKeys = [] };
        Assert.Throws<ArgumentException>(() => request.ToGrpcRequest());
    }

    [Fact]
    public void DropCollectionFieldProperties_maps_field_and_delete_keys()
    {
        var request = new DropCollectionFieldPropertiesReq
        {
            CollectionName = "book",
            FieldName = "title",
            DeleteKeys = new[] { "analyzer_params" }
        };

        Grpc.AlterCollectionFieldRequest grpc = request.ToGrpcRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("title", grpc.FieldName);
        Assert.Equal(new[] { "analyzer_params" }, grpc.DeleteKeys);
    }

    [Fact]
    public void TruncateCollection_maps_collection_name()
    {
        var request = new TruncateCollectionReq { CollectionName = "book" };
        Grpc.TruncateCollectionRequest grpc = request.ToGrpcRequest();
        Assert.Equal("book", grpc.CollectionName);
    }

    [Fact]
    public void RefreshLoad_sets_refresh_flag()
    {
        var request = new RefreshLoadReq { CollectionName = "book" };
        Grpc.LoadCollectionRequest grpc = request.ToGrpcRequest();
        Assert.Equal("book", grpc.CollectionName);
        Assert.True(grpc.Refresh);
    }

    [Fact]
    public void RefreshLoad_throws_when_name_blank()
    {
        var request = new RefreshLoadReq { CollectionName = " " };
        Assert.Throws<ArgumentException>(() => request.Validate());
    }

    [Fact]
    public void DescribeReplicas_maps_name_and_shard_nodes_flag()
    {
        var request = new DescribeReplicasReq { CollectionName = "book", WithShardNodes = true };
        Grpc.GetReplicasRequest grpc = request.ToGrpcRequest();
        Assert.Equal("book", grpc.CollectionName);
        Assert.True(grpc.WithShardNodes);
    }

    [Fact]
    public void LoadCollection_maps_partial_load_and_refresh_options()
    {
        var request = new LoadCollectionReq
        {
            CollectionName = "book",
            ReplicaNumber = 2,
            Refresh = true,
            LoadFields = new[] { "field1", "field2" },
            SkipLoadDynamicField = true,
            TargetResourceGroups = new[] { "rg1" }
        };

        Grpc.LoadCollectionRequest grpc = request.ToGrpcLoadCollectionRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal(2, grpc.ReplicaNumber);
        Assert.True(grpc.Refresh);
        Assert.Equal(new[] { "field1", "field2" }, grpc.LoadFields);
        Assert.True(grpc.SkipLoadDynamicField);
        Assert.Equal(new[] { "rg1" }, grpc.ResourceGroups);
    }

    [Fact]
    public void LoadCollection_omits_partial_load_options_when_unset()
    {
        var request = new LoadCollectionReq { CollectionName = "book" };

        Grpc.LoadCollectionRequest grpc = request.ToGrpcLoadCollectionRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.False(grpc.Refresh);
        Assert.False(grpc.SkipLoadDynamicField);
        Assert.Empty(grpc.LoadFields);
        Assert.Empty(grpc.ResourceGroups);
    }

    [Fact]
    public void GetLoadState_maps_partition_names()
    {
        var request = new GetLoadStateReq { CollectionName = "book", PartitionNames = new[] { "p1" } };

        Grpc.GetLoadStateRequest grpc = request.ToGrpcGetLoadStateRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal(new[] { "p1" }, grpc.PartitionNames);
    }

    [Fact]
    public void RenameCollection_maps_target_database()
    {
        var request = new RenameCollectionReq
        {
            CollectionName = "book",
            NewCollectionName = "book2",
            TargetDatabaseName = "db2"
        };

        Grpc.RenameCollectionRequest grpc = request.ToGrpcRenameCollectionRequest();

        Assert.Equal("book", grpc.OldName);
        Assert.Equal("book2", grpc.NewName);
        Assert.Equal("db2", grpc.NewDBName);
    }
}
