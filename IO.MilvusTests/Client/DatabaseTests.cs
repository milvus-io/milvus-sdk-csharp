using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public class DatabaseTests
{
    [Fact]
    public async Task Create_List_Drop()
    {
        string dbName = nameof(Create_List_Drop);

        if ((await Client.ListDatabasesAsync()).Contains(dbName))
        {
            // First drop all collections from a possible previous test run, otherwise dropping fails
            foreach (var collection in await Client.ShowCollectionsAsync(dbName: dbName))
            {
                await Client.DropCollectionAsync(collection.CollectionName, dbName);
            }

            await Client.DropDatabaseAsync(dbName);
        }

        await Client.CreateDatabaseAsync(dbName);

        await Client.CreateCollectionAsync(
            "foo",
            new[] { FieldType.Create<long>("id", isPrimaryKey: true) },
            dbName: dbName);
        Assert.Contains(dbName, await Client.ListDatabasesAsync());

        await Client.DropCollectionAsync("foo", dbName);
        await Client.DropDatabaseAsync(dbName);

        await Assert.ThrowsAsync<MilvusException>(() =>
            Client.CreateCollectionAsync(
                "foo",
                new[] { FieldType.Create<long>("id", isPrimaryKey: true) },
                dbName: dbName));
        Assert.DoesNotContain(dbName, await Client.ListDatabasesAsync());
    }

    [Fact]
    public async Task List()
    {
        string dbName = nameof(List);

        await Client.DropDatabaseAsync(nameof(List));
        Assert.DoesNotContain(dbName, await Client.ListDatabasesAsync());

        await Client.CreateDatabaseAsync(nameof(List));
        Assert.Contains(dbName, await Client.ListDatabasesAsync());
    }

    private MilvusClient Client => TestEnvironment.Client;
}
