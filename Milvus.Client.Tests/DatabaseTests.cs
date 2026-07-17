using Xunit;

namespace Milvus.Client.Tests;

public class DatabaseTests(MilvusFixture milvusFixture) : IAsyncLifetime
{
    [Fact]
    public async Task Create_List_Drop()
    {
        Assert.DoesNotContain(DatabaseName, await DefaultClient.ListDatabasesAsync(TestContext.Current.CancellationToken));

        await DefaultClient.CreateDatabaseAsync(DatabaseName, TestContext.Current.CancellationToken);

        Assert.Contains(DatabaseName, await DefaultClient.ListDatabasesAsync(TestContext.Current.CancellationToken));

        MilvusCollection collection = await DatabaseClient.CreateCollectionAsync(
            "foo",
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("vector", dimension: 2)
            }, cancellationToken: TestContext.Current.CancellationToken);

        // The collection should be visible on the database-bound client, but not on the default client.
        Assert.True(await DatabaseClient.HasCollectionAsync("foo", cancellationToken: TestContext.Current.CancellationToken));
        Assert.False(await DefaultClient.HasCollectionAsync("foo", cancellationToken: TestContext.Current.CancellationToken));

        await collection.DropAsync(TestContext.Current.CancellationToken);
        await DatabaseClient.DropDatabaseAsync(DatabaseName, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<MilvusException>(() =>
            DatabaseClient.CreateCollectionAsync(
                "foo",
                new[]
                {
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.DoesNotContain(DatabaseName, await DefaultClient.ListDatabasesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Search_on_non_default_database()
    {
        string databaseName = nameof(Search_on_non_default_database);

        using var databaseClient = milvusFixture.CreateClient(databaseName);

        // If the database exists, drop it using the regular client and recreate it.
        if ((await DefaultClient.ListDatabasesAsync(TestContext.Current.CancellationToken)).Contains(databaseName))
        {
            foreach (MilvusCollectionInfo collectionInfo in await databaseClient.ListCollectionsAsync(cancellationToken: TestContext.Current.CancellationToken))
            {
                await databaseClient.GetCollection(collectionInfo.Name).DropAsync(TestContext.Current.CancellationToken);
            }

            await DefaultClient.DropDatabaseAsync(databaseName, TestContext.Current.CancellationToken);
        }

        await DefaultClient.CreateDatabaseAsync(nameof(Search_on_non_default_database), TestContext.Current.CancellationToken);
        MilvusCollection collection = await databaseClient.CreateCollectionAsync(
            "coll",
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateFloatVector("float_vector", 2)
            }, cancellationToken: TestContext.Current.CancellationToken);

        await collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2, "float_vector_idx", new Dictionary<string, string>(), TestContext.Current.CancellationToken);

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
            }, cancellationToken: TestContext.Current.CancellationToken);

        await collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1), cancellationToken: TestContext.Current.CancellationToken);

        var results = await collection.SearchAsync(
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            SimilarityMetricType.L2,
            limit: 2, cancellationToken: TestContext.Current.CancellationToken);

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

    public async ValueTask InitializeAsync()
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

    public ValueTask DisposeAsync()
    {
        DefaultClient.Dispose();
        DatabaseClient.Dispose();
        return ValueTask.CompletedTask;
    }

    private readonly MilvusClient DefaultClient = milvusFixture.CreateClient();
    private readonly MilvusClient DatabaseClient = milvusFixture.CreateClient(DatabaseName);

    private const string DatabaseName = nameof(DatabaseTests);
}
