using Xunit;

using Milvus.Client.V2.Responses.Aliases;
using Milvus.Client.V2.Responses.Database;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Response;

[Trait("Category", "Unit")]
public class AliasDatabaseRespTests
{
    [Fact]
    public void DescribeAlias_from_grpc_collects_collection_and_alias()
    {
        var grpc = new Grpc.DescribeAliasResponse
        {
            Collection = "book",
            Alias = "book_alias"
        };

        DescribeAliasResp response = DescribeAliasResp.FromGrpc(grpc);

        Assert.Equal("book", response.CollectionName);
        Assert.Equal("book_alias", response.Alias);
    }

    [Fact]
    public void ListAliases_from_grpc_collects_aliases()
    {
        var grpc = new Grpc.ListAliasesResponse();
        grpc.Aliases.Add("alias_a");
        grpc.Aliases.Add("alias_b");

        ListAliasesResp response = ListAliasesResp.FromGrpc(grpc);

        Assert.Equal(new[] { "alias_a", "alias_b" }, response.Aliases);
    }

    [Fact]
    public void DescribeDatabase_from_grpc_collects_name_id_timestamp_and_properties()
    {
        var grpc = new Grpc.DescribeDatabaseResponse
        {
            DbName = "mydb",
            DbID = 7,
            CreatedTimestamp = 1234567890UL
        };
        grpc.Properties.Add(new Grpc.KeyValuePair { Key = "key1", Value = "value1" });
        grpc.Properties.Add(new Grpc.KeyValuePair { Key = "key2", Value = "value2" });

        DescribeDatabaseResp response = DescribeDatabaseResp.FromGrpc(grpc);

        Assert.Equal("mydb", response.DatabaseName);
        Assert.Equal(7, response.DbId);
        Assert.Equal(1234567890UL, response.CreatedTimestamp);
        Assert.Equal(2, response.Properties.Count);
        Assert.Equal("value1", response.Properties["key1"]);
        Assert.Equal("value2", response.Properties["key2"]);
    }

    [Fact]
    public void ListDatabases_from_grpc_collects_db_names()
    {
        var grpc = new Grpc.ListDatabasesResponse();
        grpc.DbNames.Add("db_a");
        grpc.DbNames.Add("db_b");
        grpc.CreatedTimestamp.Add(1000);
        grpc.CreatedTimestamp.Add(2000);
        grpc.DbIds.Add(11);
        grpc.DbIds.Add(22);

        ListDatabasesResp response = ListDatabasesResp.FromGrpc(grpc);

        Assert.Equal(new[] { "db_a", "db_b" }, response.DatabaseNames);
        Assert.Equal(new[] { 1000UL, 2000UL }, response.CreatedTimestamps);
        Assert.Equal(new[] { 11L, 22L }, response.DatabaseIds);
    }
}
