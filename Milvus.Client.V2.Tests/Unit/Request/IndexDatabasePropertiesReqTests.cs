using Xunit;

using Milvus.Client.V2.Requests.Database;
using Milvus.Client.V2.Requests.Index;

namespace Milvus.Client.V2.Tests.Unit.Request;

[Trait("Category", "Unit")]
public class IndexDatabasePropertiesReqTests
{
    [Fact]
    public void AlterIndexProperties_maps_extra_params_and_delete_keys()
    {
        var request = new AlterIndexPropertiesReq
        {
            CollectionName = "book",
            IndexName = "my_idx",
        };
        request.Properties["pq.m"] = "16";
        request.DeleteKeys = new[] { "old_key" };

        Grpc.AlterIndexRequest grpc = request.ToGrpcRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("my_idx", grpc.IndexName);
        Assert.Equal("16", grpc.ExtraParams.Single(p => p.Key == "pq.m").Value);
        Assert.Equal(new[] { "old_key" }, grpc.DeleteKeys);
    }

    [Fact]
    public void AlterIndexProperties_defaults_index_name()
    {
        var request = new AlterIndexPropertiesReq { CollectionName = "book" };
        Grpc.AlterIndexRequest grpc = request.ToGrpcRequest();
        Assert.Equal("_default_idx", grpc.IndexName);
    }

    [Fact]
    public void DropIndexProperties_maps_delete_keys()
    {
        var request = new DropIndexPropertiesReq
        {
            CollectionName = "book",
            DeleteKeys = new[] { "pq.m" }
        };

        Grpc.AlterIndexRequest grpc = request.ToGrpcRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Empty(grpc.ExtraParams);
        Assert.Equal(new[] { "pq.m" }, grpc.DeleteKeys);
    }

    [Fact]
    public void DropIndexProperties_throws_when_delete_keys_empty()
    {
        var request = new DropIndexPropertiesReq { CollectionName = "book", DeleteKeys = [] };
        Assert.Throws<ArgumentException>(() => request.ToGrpcRequest());
    }

    [Fact]
    public void AlterDatabaseProperties_maps_properties_and_delete_keys()
    {
        var request = new AlterDatabasePropertiesReq
        {
            DatabaseName = "mydb",
        };
        request.Properties["database.max.replicas"] = "3";
        request.DeleteKeys = new[] { "old_key" };

        Grpc.AlterDatabaseRequest grpc = request.ToGrpcRequest();

        Assert.Equal("mydb", grpc.DbName);
        Assert.Equal("3", grpc.Properties.Single(p => p.Key == "database.max.replicas").Value);
        Assert.Equal(new[] { "old_key" }, grpc.DeleteKeys);
    }

    [Fact]
    public void DropDatabaseProperties_maps_delete_keys()
    {
        var request = new DropDatabasePropertiesReq
        {
            DatabaseName = "mydb",
            DeleteKeys = new[] { "database.max.replicas" }
        };

        Grpc.AlterDatabaseRequest grpc = request.ToGrpcRequest();

        Assert.Equal("mydb", grpc.DbName);
        Assert.Empty(grpc.Properties);
        Assert.Equal(new[] { "database.max.replicas" }, grpc.DeleteKeys);
    }

    [Fact]
    public void DropDatabaseProperties_throws_when_delete_keys_empty()
    {
        var request = new DropDatabasePropertiesReq { DatabaseName = "mydb", DeleteKeys = [] };
        Assert.Throws<ArgumentException>(() => request.ToGrpcRequest());
    }
}
