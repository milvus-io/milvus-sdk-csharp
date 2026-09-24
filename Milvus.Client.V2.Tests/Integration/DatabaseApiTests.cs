using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Database;
using Milvus.Client.V2.Responses.Database;
using Milvus.Client.Grpc;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class DatabaseApiTests
{
    // ---- Database ----

    [Fact]
    public async Task CreateDatabase_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreateDatabaseAsync(
            new CreateDatabaseReq { DatabaseName = "db1" },
            TestContext.Current.CancellationToken);

        CreateDatabaseRequest request = Assert.IsType<CreateDatabaseRequest>(server.Service.Requests["CreateDatabase"]);
        Assert.Equal("db1", request.DbName);
    }

    [Fact]
    public async Task DropDatabase_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropDatabaseAsync(
            new DropDatabaseReq { DatabaseName = "db1" },
            TestContext.Current.CancellationToken);

        DropDatabaseRequest request = Assert.IsType<DropDatabaseRequest>(server.Service.Requests["DropDatabase"]);
        Assert.Equal("db1", request.DbName);
    }

    [Fact]
    public async Task DescribeDatabase_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        DescribeDatabaseResp response = await client.DescribeDatabaseAsync(
            new DescribeDatabaseReq { DatabaseName = "db1" },
            TestContext.Current.CancellationToken);

        DescribeDatabaseRequest request = Assert.IsType<DescribeDatabaseRequest>(server.Service.Requests["DescribeDatabase"]);
        Assert.Equal("db1", request.DbName);
        Assert.Equal("db1", response.DatabaseName);
    }

    [Fact]
    public async Task ListDatabases_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.ListDatabasesAsync(
            new ListDatabasesReq(),
            TestContext.Current.CancellationToken);

        Assert.IsType<ListDatabasesRequest>(server.Service.Requests["ListDatabases"]);
    }

    [Fact]
    public async Task AlterDatabaseProperties_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.AlterDatabasePropertiesAsync(
            new AlterDatabasePropertiesReq
            {
                DatabaseName = "db1",
                Properties = { { "replica.number", "2" } },
                DeleteKeys = new[] { "old_key" }
            },
            TestContext.Current.CancellationToken);

        AlterDatabaseRequest request = Assert.IsType<AlterDatabaseRequest>(server.Service.Requests["AlterDatabase"]);
        Assert.Equal("db1", request.DbName);
        Assert.Contains(request.Properties, p => p.Key == "replica.number" && p.Value == "2");
        Assert.Contains("old_key", request.DeleteKeys);
    }

    [Fact]
    public async Task DropDatabaseProperties_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropDatabasePropertiesAsync(
            new DropDatabasePropertiesReq
            {
                DatabaseName = "db1",
                DeleteKeys = new[] { "key1", "key2" }
            },
            TestContext.Current.CancellationToken);

        AlterDatabaseRequest request = Assert.IsType<AlterDatabaseRequest>(server.Service.Requests["AlterDatabase"]);
        Assert.Equal("db1", request.DbName);
        Assert.Equal(new[] { "key1", "key2" }, request.DeleteKeys);
        Assert.Empty(request.Properties);
    }
}
