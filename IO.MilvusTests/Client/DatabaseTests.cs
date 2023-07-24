using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public class DatabaseTests : IAsyncLifetime
{
    [Fact]
    public async Task Create_List_Drop()
    {
        Assert.DoesNotContain(DatabaseName, await Client.ListDatabasesAsync());

        MilvusDatabase database = await Client.CreateDatabaseAsync(DatabaseName);

        Assert.Contains(DatabaseName, await Client.ListDatabasesAsync());

        MilvusCollection collection = await database.CreateCollectionAsync(
            "foo",
            new[] { FieldSchema.Create<long>("id", isPrimaryKey: true) });

        Assert.True(await database.HasCollectionAsync("foo"));
        Assert.False(await Client.HasCollectionAsync("foo"));

        await collection.DropAsync();
        await database.DropAsync();

        await Assert.ThrowsAsync<MilvusException>(() =>
            database.CreateCollectionAsync(
                "foo",
                new[] { FieldSchema.Create<long>("id", isPrimaryKey: true) }));
        Assert.DoesNotContain(DatabaseName, await Client.ListDatabasesAsync());
    }

    public async Task InitializeAsync()
    {
        if ((await Client.ListDatabasesAsync()).Contains(DatabaseName))
        {
            MilvusDatabase database = Client.GetDatabase(DatabaseName);

            // First drop all collections from a possible previous test run, otherwise dropping fails
            foreach (var collection in await database.ShowCollectionsAsync())
            {
                await database.GetCollection(collection.Name).DropAsync();
            }

            await database.DropAsync();
        }
    }

    public Task DisposeAsync()
        => Task.CompletedTask;

    private MilvusClient Client => TestEnvironment.Client;

    private const string DatabaseName = nameof(DatabaseTests);
}
