using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public class DatabaseTests
{
    [Fact]
    public async Task Create_List_Drop()
    {
        if ((await Client.ListDatabasesAsync()).Contains(DatabaseName))
        {
            // First drop all collections from a possible previous test run, otherwise dropping fails
            foreach (var collection in await Client.ShowCollectionsAsync(dbName: DatabaseName))
            {
                await Client.DropCollectionAsync(collection.CollectionName, DatabaseName);
            }

            await Client.DropDatabaseAsync(DatabaseName);
        }

        await Client.CreateDatabaseAsync(DatabaseName);

        await Client.CreateCollectionAsync(
            "foo",
            new[] { FieldSchema.Create<long>("id", isPrimaryKey: true) },
            dbName: DatabaseName);
        Assert.Contains(DatabaseName, await Client.ListDatabasesAsync());

        await Client.DropCollectionAsync("foo", DatabaseName);
        await Client.DropDatabaseAsync(DatabaseName);

        await Assert.ThrowsAsync<MilvusException>(() =>
            Client.CreateCollectionAsync(
                "foo",
                new[] { FieldSchema.Create<long>("id", isPrimaryKey: true) },
                dbName: DatabaseName));
        Assert.DoesNotContain(DatabaseName, await Client.ListDatabasesAsync());
    }

    [Fact]
    public async Task List()
    {
        await Client.DropDatabaseAsync(DatabaseName);
        Assert.DoesNotContain(DatabaseName, await Client.ListDatabasesAsync());

        await Client.CreateDatabaseAsync(DatabaseName);
        Assert.Contains(DatabaseName, await Client.ListDatabasesAsync());
    }

    public string DatabaseName = nameof(DatabaseTests);

    private MilvusClient Client => TestEnvironment.Client;
}
