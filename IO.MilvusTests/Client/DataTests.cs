using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public class DataTests : IAsyncLifetime
{
    [Fact]
    public async Task Insert_Drop()
    {
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });

        MilvusMutationResult mutationResult = await Client.InsertAsync(
            CollectionName,
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

        await Client.CreateIndexAsync(
            CollectionName, "float_vector", MilvusIndexType.Flat, MilvusSimilarityMetricType.L2);
        await Client.WaitForIndexBuildAsync(CollectionName, "id");
        await Client.LoadCollectionAsync(CollectionName);
        await Client.WaitForCollectionLoadAsync(CollectionName);

        var queryResult = await Client.QueryAsync(CollectionName, "id in [2]");

        var result = Assert.IsType<FieldData<long>>(Assert.Single(queryResult.FieldsData));
        Assert.Equal(2, Assert.Single(result.Data));

        mutationResult = await Client.DeleteAsync(CollectionName, "id in [2]");
        Assert.Collection(mutationResult.Ids.LongIds!, i => Assert.Equal(2, i));
        Assert.Equal(1, mutationResult.DeleteCount);
        Assert.Equal(0, mutationResult.InsertCount);
        Assert.Equal(0, mutationResult.UpsertCount);
        ulong timestamp = mutationResult.Timestamp;

        queryResult = await Client.QueryAsync(
            CollectionName, "id in [2]", consistencyLevel: ConsistencyLevel.Customized, guaranteeTimestamp: timestamp);
        result = Assert.IsType<FieldData<long>>(Assert.Single(queryResult.FieldsData));
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task Timestamp_conversion()
    {
        DateTime before = DateTime.UtcNow;

        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });

        MilvusMutationResult mutationResult = await Client.InsertAsync(
            CollectionName,
            new FieldData[]
            {
                FieldData.Create("id", new long[] { 1, 2 }),
                FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
                {
                    new[] { 1f, 2f },
                    new[] { 3f, 4f }
                })
            });

        DateTime after = DateTime.UtcNow;

        DateTime insertion = MilvusTimestampUtils.ToDateTime(mutationResult.Timestamp);

        Assert.True(insertion >= before);
        Assert.True(insertion <= after);

        // Note that Milvus timestamps have a logical component that gets stripped away when we convert to DateTime,
        // so converting back doesn't yield the same result. Search_with_time_travel exercises the other direction.
    }

    public async Task InitializeAsync()
        => await Client.DropCollectionAsync(CollectionName);

    public Task DisposeAsync()
        => Task.CompletedTask;

    private const string CollectionName = nameof(DataTests);
    private MilvusClient Client => TestEnvironment.Client;
}
