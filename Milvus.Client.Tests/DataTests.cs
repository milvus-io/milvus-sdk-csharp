using Xunit;

namespace Milvus.Client.Tests;

public class DataTests : IClassFixture<DataTests.DataCollectionFixture>
{
    [Fact]
    public async Task Insert_Drop()
    {
        MilvusMutationResult mutationResult = await InsertData(1, 2);

        Assert.Collection(mutationResult.Ids.LongIds!,
            i => Assert.Equal(1, i),
            i => Assert.Equal(2, i));
        Assert.Equal(0, mutationResult.DeleteCount);
        Assert.Equal(2, mutationResult.InsertCount);
        Assert.Equal(0, mutationResult.UpsertCount);

        IReadOnlyList<FieldData> results = await Collection.QueryAsync(
            "id in [2]",
            consistencyLevel: ConsistencyLevel.Strong);

        FieldData<long> result = Assert.IsType<FieldData<long>>(Assert.Single(results));
        Assert.Equal(2, Assert.Single(result.Data));

        mutationResult = await Collection.DeleteAsync("id in [2]");
        Assert.Collection(mutationResult.Ids.LongIds!, i => Assert.Equal(2, i));
        Assert.Equal(1, mutationResult.DeleteCount);
        Assert.Equal(0, mutationResult.InsertCount);
        Assert.Equal(0, mutationResult.UpsertCount);
        ulong timestamp = mutationResult.Timestamp;

        results = await Collection.QueryAsync(
            "id in [2]", consistencyLevel: ConsistencyLevel.Customized, guaranteeTimestamp: timestamp);
        result = Assert.IsType<FieldData<long>>(Assert.Single(results));
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

        MilvusMutationResult mutationResult = await InsertData(3, 4);

        DateTime insertion = MilvusTimestampUtils.ToDateTime(mutationResult.Timestamp);

        await Task.Delay(100);

        DateTime after = DateTime.UtcNow;

        Assert.True(insertion >= before, $"Insertion timestamp {insertion} was not after timestamp {before}");
        Assert.True(insertion <= after, $"Insertion timestamp {insertion} was not before timestamp {after}");

        // Note that Milvus timestamps have a logical component that gets stripped away when we convert to DateTime,
        // so converting back doesn't yield the same result. Search_with_time_travel exercises the other direction.
    }

    [Fact]
    public async Task Flush()
    {
        await Collection.WaitForFlushAsync();
        // Any insertion after a flush operation results in generating new segments.
        await InsertData(5, 6);
        MilvusFlushResult newResult = await Collection.FlushAsync();

        Assert.NotEmpty(newResult.CollSegIDs);
        Assert.Equal(CollectionName, newResult.CollSegIDs.First().Key);
        Assert.NotEmpty(newResult.CollSegIDs.First().Value);

        await Collection.WaitForFlushAsync();
    }

    [Fact]
    public async Task Flush_with_not_exist_collection()
    {
        await Assert.ThrowsAsync<MilvusException>(() => Client.FlushAsync(new[] { "NotExist" }));
    }

    [Fact]
    public async Task GetFlushState_with_empty_ids()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Client.GetFlushStateAsync(Array.Empty<long>()));
    }

    [Fact]
    public async Task GetFlushState_with_not_exist_ids()
    {
        // But return true.
        bool result = await Client.GetFlushStateAsync(new long[] { -1, -2, -3 });
        Assert.True(result);
    }

    [Fact]
    public async Task Collection_waitForFlush()
    {
        MilvusCollectionDescription collectionDes = await Collection.DescribeAsync();
        await InsertData(7, 8);
        await Collection.WaitForFlushAsync();
        IEnumerable<MilvusPersistentSegmentInfo> segmentInfos = await Collection.GetPersistentSegmentInfosAsync();

        MilvusPersistentSegmentInfo? segmentInfo = segmentInfos.LastOrDefault();

        Assert.NotNull(segmentInfo);
        Assert.Equal(MilvusSegmentState.Flushed, segmentInfo.State);
        Assert.True(segmentInfo.NumRows > 0);
        Assert.Equal(collectionDes.CollectionId, segmentInfo.CollectionId);
    }

    [Fact]
    public async Task FlushAll_wait()
    {
        // This method is Time consuming.
        await InsertData(9, 10);

        await Task.Delay(100);

        // Flush all
        ulong flushAllTs = await Client.FlushAllAsync();

        // Test if it is a timestamp
        DateTime flushAllDateTime = MilvusTimestampUtils.ToDateTime(flushAllTs);
        Assert.True(flushAllDateTime - DateTime.UtcNow < TimeSpan.FromMilliseconds(10));

        // Wait
        await Client.WaitForFlushAllAsync(flushAllTs);

        IEnumerable<MilvusPersistentSegmentInfo> segmentInfos = await Collection.GetPersistentSegmentInfosAsync();
        Assert.True(segmentInfos.All(p => p.State == MilvusSegmentState.Flushed));
    }

    private async Task<MilvusMutationResult> InsertData(long id1, long id2)
        => await Collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new []{ id1, id2 }),
                FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
                {
                    new[] { 1f, 2f },
                    new[] { 3f, 4f }
                })
            });

    public class DataCollectionFixture : IAsyncLifetime
    {
        public MilvusCollection Collection { get; }

        public DataCollectionFixture()
            => Collection = TestEnvironment.Client.GetCollection(CollectionName);

        public async Task InitializeAsync()
        {
            await Collection.DropAsync();

            await TestEnvironment.Client.CreateCollectionAsync(
                Collection.Name,
                new[]
                {
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("float_vector", 2)
                });

            await Collection.CreateIndexAsync("float_vector", MilvusIndexType.Flat, MilvusSimilarityMetricType.L2);
            await Collection.WaitForIndexBuildAsync("float_vector");
            await Collection.LoadAsync();
            await Collection.WaitForCollectionLoadAsync();
        }

        public Task DisposeAsync()
            => Task.CompletedTask;
    }

    private readonly DataCollectionFixture _dataCollectionFixture;
    private const string CollectionName = nameof(DataTests);
    private MilvusCollection Collection => _dataCollectionFixture.Collection;
    private MilvusClient Client => TestEnvironment.Client;

    public DataTests(DataCollectionFixture dataCollectionFixture)
        => _dataCollectionFixture = dataCollectionFixture;
}
