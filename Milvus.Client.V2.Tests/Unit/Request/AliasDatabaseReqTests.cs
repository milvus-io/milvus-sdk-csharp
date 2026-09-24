using Xunit;

using Milvus.Client.V2.Requests.Aliases;
using Milvus.Client.V2.Requests.Database;

namespace Milvus.Client.V2.Tests.Unit.Request;

[Trait("Category", "Unit")]
public class AliasDatabaseReqTests
{
    [Fact]
    public void AlterAlias_maps_collection_and_alias()
    {
        var request = new AlterAliasReq
        {
            CollectionName = "book",
            Alias = "book_alias"
        };

        Grpc.AlterAliasRequest grpc = request.ToGrpcAlterAliasRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("book_alias", grpc.Alias);
    }

    [Fact]
    public void AlterAlias_throws_when_collection_blank()
    {
        var request = new AlterAliasReq { Alias = "book_alias" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcAlterAliasRequest());
    }

    [Fact]
    public void CreateAlias_maps_collection_and_alias()
    {
        var request = new CreateAliasReq
        {
            CollectionName = "book",
            Alias = "book_alias"
        };

        Grpc.CreateAliasRequest grpc = request.ToGrpcCreateAliasRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("book_alias", grpc.Alias);
    }

    [Fact]
    public void CreateAlias_throws_when_alias_blank()
    {
        var request = new CreateAliasReq { CollectionName = "book", Alias = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcCreateAliasRequest());
    }

    [Fact]
    public void DescribeAlias_maps_alias()
    {
        var request = new DescribeAliasReq { Alias = "book_alias" };

        Grpc.DescribeAliasRequest grpc = request.ToGrpcDescribeAliasRequest();

        Assert.Equal("book_alias", grpc.Alias);
    }

    [Fact]
    public void DescribeAlias_throws_when_alias_blank()
    {
        var request = new DescribeAliasReq();
        Assert.Throws<ArgumentException>(() => request.ToGrpcDescribeAliasRequest());
    }

    [Fact]
    public void DropAlias_maps_alias()
    {
        var request = new DropAliasReq { Alias = "book_alias" };

        Grpc.DropAliasRequest grpc = request.ToGrpcDropAliasRequest();

        Assert.Equal("book_alias", grpc.Alias);
    }

    [Fact]
    public void DropAlias_throws_when_alias_blank()
    {
        var request = new DropAliasReq { Alias = "\t" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDropAliasRequest());
    }

    [Fact]
    public void ListAliases_maps_collection_name()
    {
        var request = new ListAliasesReq { CollectionName = "book" };

        Grpc.ListAliasesRequest grpc = request.ToGrpcListAliasesRequest();

        Assert.Equal("book", grpc.CollectionName);
    }

    [Fact]
    public void ListAliases_defaults_null_collection_to_empty()
    {
        var request = new ListAliasesReq();

        Grpc.ListAliasesRequest grpc = request.ToGrpcListAliasesRequest();

        Assert.Equal("", grpc.CollectionName);
    }

    [Fact]
    public void CreateDatabase_maps_db_name()
    {
        var request = new CreateDatabaseReq { DatabaseName = "mydb" };
        request.Properties["key1"] = "value1";

        Grpc.CreateDatabaseRequest grpc = request.ToGrpcCreateDatabaseRequest();

        Assert.Equal("mydb", grpc.DbName);
        var properties = grpc.Properties.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("value1", properties["key1"]);
    }

    [Fact]
    public void CreateDatabase_throws_when_db_name_blank()
    {
        var request = new CreateDatabaseReq();
        Assert.Throws<ArgumentException>(() => request.ToGrpcCreateDatabaseRequest());
    }

    [Fact]
    public void DescribeDatabase_maps_db_name()
    {
        var request = new DescribeDatabaseReq { DatabaseName = "mydb" };

        Grpc.DescribeDatabaseRequest grpc = request.ToGrpcDescribeDatabaseRequest();

        Assert.Equal("mydb", grpc.DbName);
    }

    [Fact]
    public void DescribeDatabase_throws_when_db_name_blank()
    {
        var request = new DescribeDatabaseReq { DatabaseName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDescribeDatabaseRequest());
    }

    [Fact]
    public void DropDatabase_maps_db_name()
    {
        var request = new DropDatabaseReq { DatabaseName = "mydb" };

        Grpc.DropDatabaseRequest grpc = request.ToGrpcDropDatabaseRequest();

        Assert.Equal("mydb", grpc.DbName);
    }

    [Fact]
    public void DropDatabase_throws_when_db_name_blank()
    {
        var request = new DropDatabaseReq { DatabaseName = "" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDropDatabaseRequest());
    }

    [Fact]
    public void ListDatabases_builds_empty_request()
    {
        Grpc.ListDatabasesRequest grpc = ListDatabasesReq.ToGrpcListDatabasesRequest();
        Assert.NotNull(grpc);
    }
}
