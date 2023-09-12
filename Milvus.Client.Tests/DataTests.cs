using Xunit;

namespace Milvus.Client.Tests;

public class DataTests : IClassFixture<DataTests.DataCollectionFixture>
{
    [Fact]
    public async Task Insert_Drop()
    {
        MutationResult mutationResult = await InsertDataAsync(1, 2);

        Assert.Collection(mutationResult.Ids.LongIds!,
            i => Assert.Equal(1, i),
            i => Assert.Equal(2, i));
        Assert.Equal(0, mutationResult.DeleteCount);
        Assert.Equal(2, mutationResult.InsertCount);
        Assert.Equal(0, mutationResult.UpsertCount);

        IReadOnlyList<FieldData> results = await Collection.QueryAsync(
            "id in [2]",
            new() { ConsistencyLevel = ConsistencyLevel.Strong });

        FieldData<long> result = Assert.IsType<FieldData<long>>(Assert.Single(results));
        Assert.Equal(2, Assert.Single(result.Data));

        mutationResult = await Collection.DeleteAsync("id in [2]");
        Assert.Collection(mutationResult.Ids.LongIds!, i => Assert.Equal(2, i));
        Assert.Equal(1, mutationResult.DeleteCount);
        Assert.Equal(0, mutationResult.InsertCount);
        Assert.Equal(0, mutationResult.UpsertCount);
        ulong timestamp = mutationResult.Timestamp;

        results = await Collection.QueryAsync(
            "id in [2]",
            new()
            {
                ConsistencyLevel = ConsistencyLevel.Customized,
                GuaranteeTimestamp = timestamp
            });
        result = Assert.IsType<FieldData<long>>(Assert.Single(results));
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task Timestamp_conversion()
    {
        DateTime before = DateTime.UtcNow;

        await Task.Delay(100);

        MutationResult mutationResult = await InsertDataAsync(3, 4);

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
        await InsertDataAsync(5, 6);
        FlushResult newResult = await Collection.FlushAsync();

        Assert.NotEmpty(newResult.CollSegIDs);
        Assert.Equal(CollectionName, newResult.CollSegIDs.First().Key);
        Assert.NotEmpty(newResult.CollSegIDs.First().Value);

        await Collection.WaitForFlushAsync();
    }

    [Fact]
    public async Task Flush_with_not_exist_collection()
        => await Assert.ThrowsAsync<MilvusException>(() => Client.FlushAsync(new[] { "NotExist" }));

    [Fact]
    public async Task GetFlushState_with_empty_ids()
        => await Assert.ThrowsAsync<ArgumentException>(() => Client.GetFlushStateAsync(Array.Empty<long>()));

    [Fact]
    public async Task GetFlushState_with_not_exist_ids()
        => Assert.True(await Client.GetFlushStateAsync(new long[] { -1, -2, -3 })); // But return true.

    [Fact]
    public async Task Collection_waitForFlush()
    {
        MilvusCollectionDescription collectionDes = await Collection.DescribeAsync();
        await InsertDataAsync(7, 8);
        await Collection.WaitForFlushAsync();
        IEnumerable<PersistentSegmentInfo> segmentInfos = await Collection.GetPersistentSegmentInfosAsync();

        PersistentSegmentInfo? segmentInfo = segmentInfos.LastOrDefault();

        Assert.NotNull(segmentInfo);
        Assert.Equal(SegmentState.Flushed, segmentInfo.State);
        Assert.True(segmentInfo.NumRows > 0);
        Assert.Equal(collectionDes.CollectionId, segmentInfo.CollectionId);
    }

    [Fact]
    public async Task FlushAllAsync_and_wait()
    {
        // Waiting for FlushAllAsync can take around a minute.
        // To make the developer inner loop quicker, we run this test only on CI.
        if (Environment.GetEnvironmentVariable("CI") == null)
        {
            return;
        }

        await InsertDataAsync(9, 10);

        // Flush all
        ulong timestamp = await Client.FlushAllAsync();

        // Test if it is a timestamp
        DateTime flushAllDateTime = MilvusTimestampUtils.ToDateTime(timestamp);
        Assert.True(flushAllDateTime - DateTime.UtcNow < TimeSpan.FromMilliseconds(10));

        // Wait
        await Client.WaitForFlushAllAsync(timestamp);

        IEnumerable<PersistentSegmentInfo> segmentInfos = await Collection.GetPersistentSegmentInfosAsync();
        Assert.True(segmentInfos.All(p => p.State == SegmentState.Flushed));
    }

    [Fact]
    public async Task Insert_dynamic_field()
    {
        await Collection.DropAsync();

        await TestEnvironment.Client.CreateCollectionAsync(
            Collection.Name,
            new CollectionSchema
            {
                Fields =
                {
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("float_vector", 2)
                },
                EnableDynamicFields = true
            });

        await Collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new[] { 1L, 2L }),
                FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
                {
                    new[] { 1f, 2f },
                    new[] { 3f, 4f }
                }),
                FieldData.CreateVarChar("unknown_varchar", new[] { "dynamic str1", "dynamic str2" }, isDynamic: true),
                FieldData.Create("unknown_int", new[] { 8L, 9L }, isDynamic: true)
            });
    }

    private async Task<MutationResult> InsertDataAsync(long id1, long id2)
        => await Collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new[] { id1, id2 }),
                FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
                {
                    new[] { 1f, 2f },
                    new[] { 3f, 4f }
                })
            });

    public class DataCollectionFixture : IAsyncLifetime
    {
        public MilvusCollection Collection { get; } = TestEnvironment.Client.GetCollection(CollectionName);

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

            await Collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);
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
