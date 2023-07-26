using Xunit;

namespace Milvus.Client.Tests;

public class DataTests : IAsyncLifetime
{
    [Fact]
    public async Task Insert_Drop()
    {
        MilvusCollection collection = await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });

        MilvusMutationResult mutationResult = await collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new long[] { 1, 2 }),
                FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
                {
                    new[] { 1f, 2f },
                    new[] { 3f, 4f }
                })
            });

        Assert.Collection(mutationResult.Ids.LongIds!,
            i => Assert.Equal(1, i),
            i => Assert.Equal(2, i));
        Assert.Equal(0, mutationResult.DeleteCount);
        Assert.Equal(2, mutationResult.InsertCount);
        Assert.Equal(0, mutationResult.UpsertCount);

        await collection.CreateIndexAsync("float_vector", MilvusIndexType.Flat, MilvusSimilarityMetricType.L2);
        await collection.WaitForIndexBuildAsync("id");
        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync();

        var queryResult = await collection.QueryAsync("id in [2]");

        var result = Assert.IsType<FieldData<long>>(Assert.Single(queryResult.FieldsData));
        Assert.Equal(2, Assert.Single(result.Data));

        mutationResult = await collection.DeleteAsync("id in [2]");
        Assert.Collection(mutationResult.Ids.LongIds!, i => Assert.Equal(2, i));
        Assert.Equal(1, mutationResult.DeleteCount);
        Assert.Equal(0, mutationResult.InsertCount);
        Assert.Equal(0, mutationResult.UpsertCount);
        ulong timestamp = mutationResult.Timestamp;

        queryResult = await collection.QueryAsync(
            "id in [2]", consistencyLevel: ConsistencyLevel.Customized, guaranteeTimestamp: timestamp);
        result = Assert.IsType<FieldData<long>>(Assert.Single(queryResult.FieldsData));
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task Timestamp_conversion()
    {
        DateTime before = DateTime.UtcNow;

        MilvusCollection collection = await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });

        await Task.Delay(100);

        MilvusMutationResult mutationResult = await collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new long[] { 1, 2 }),
                FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
                {
                    new[] { 1f, 2f },
                    new[] { 3f, 4f }
                })
            });

        DateTime insertion = MilvusTimestampUtils.ToDateTime(mutationResult.Timestamp);

        await Task.Delay(100);

        DateTime after = DateTime.UtcNow;

        Assert.True(insertion >= before, $"Insertion timestamp {insertion} was not after timestamp {before}");
        Assert.True(insertion <= after, $"Insertion timestamp {insertion} was not before timestamp {after}");

        // Note that Milvus timestamps have a logical component that gets stripped away when we convert to DateTime,
        // so converting back doesn't yield the same result. Search_with_time_travel exercises the other direction.
    }

    public async Task InitializeAsync()
        => await Client.GetCollection(CollectionName).DropAsync();

    public Task DisposeAsync()
        => Task.CompletedTask;

    private const string CollectionName = nameof(DataTests);
    private MilvusClient Client => TestEnvironment.Client;
}
