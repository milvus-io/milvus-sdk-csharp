using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class DatabaseTests(MilvusFixture milvusFixture) : IAsyncLifetime
{
    [Fact]
    public async Task Create_List_Drop()
    {
        Assert.DoesNotContain(DatabaseName, await DefaultClient.ListDatabasesAsync());

        await DefaultClient.CreateDatabaseAsync(DatabaseName);

        Assert.Contains(DatabaseName, await DefaultClient.ListDatabasesAsync());

        MilvusCollection collection = await DatabaseClient.CreateCollectionAsync(
            "foo",
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("vector", dimension: 2)
            });

        // The collection should be visible on the database-bound client, but not on the default client.
        Assert.True(await DatabaseClient.HasCollectionAsync("foo"));
        Assert.False(await DefaultClient.HasCollectionAsync("foo"));

        await collection.DropAsync();
        await DatabaseClient.DropDatabaseAsync(DatabaseName);

        await Assert.ThrowsAsync<MilvusException>(() =>
            DatabaseClient.CreateCollectionAsync(
                "foo",
                new[]
                {
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }));
        Assert.DoesNotContain(DatabaseName, await DefaultClient.ListDatabasesAsync());
    }

    [Fact]
    public async Task Search_on_non_default_database()
    {
        string databaseName = nameof(Search_on_non_default_database);

        using var databaseClient = milvusFixture.CreateClient(databaseName);

        // If the database exists, drop it using the regular client and recreate it.
        if ((await DefaultClient.ListDatabasesAsync()).Contains(databaseName))
        {
            foreach (MilvusCollectionInfo collectionInfo in await databaseClient.ListCollectionsAsync())
            {
                await databaseClient.GetCollection(collectionInfo.Name).DropAsync();
            }

            await DefaultClient.DropDatabaseAsync(databaseName);
        }

        await DefaultClient.CreateDatabaseAsync(nameof(Search_on_non_default_database));
        MilvusCollection collection = await databaseClient.CreateCollectionAsync(
            "coll",
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });

        await collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2, "float_vector_idx", new Dictionary<string, string>());

        long[] ids = { 1, 2, 3, 4, 5 };
        string[] strings = { "one", "two", "three", "four", "five" };
        ReadOnlyMemory<float>[] floatVectors =
        {
            new[] { 1f, 2f },
            new[] { 3.5f, 4.5f },
            new[] { 5f, 6f },
            new[] { 7.7f, 8.8f },
            new[] { 9f, 10f }
        };

        await collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", ids),
                FieldData.Create("varchar", strings),
                FieldData.CreateFloatVector("float_vector", floatVectors)
            });

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        var results = await collection.SearchAsync(
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            SimilarityMetricType.L2,
            limit: 2);

        Assert.Equal(collection.Name, results.CollectionName);
        Assert.Empty(results.FieldsData);
        Assert.Collection(results.Ids.LongIds!,
            id => Assert.Equal(1, id),
            id => Assert.Equal(2, id));
        Assert.Null(results.Ids.StringIds);
        Assert.Equal(1, results.NumQueries);
        Assert.Equal(2, results.Scores.Count);
        Assert.Equal(2, results.Limit);
        Assert.Collection(results.Limits, l => Assert.Equal(2, l));
    }

    public async Task InitializeAsync()
    {
        if ((await DefaultClient.ListDatabasesAsync()).Contains(DatabaseName))
        {
            // First drop all collections from a possible previous test run, otherwise dropping fails
            foreach (var collection in await DatabaseClient.ListCollectionsAsync())
            {
                await DatabaseClient.GetCollection(collection.Name).DropAsync();
            }

            await DefaultClient.DropDatabaseAsync(DatabaseName);
        }
    }

    public Task DisposeAsync()
    {
        DefaultClient.Dispose();
        DatabaseClient.Dispose();
        return Task.CompletedTask;
    }

    private readonly MilvusClient DefaultClient = milvusFixture.CreateClient();
    private readonly MilvusClient DatabaseClient = milvusFixture.CreateClient(DatabaseName);

    private const string DatabaseName = nameof(DatabaseTests);
}
