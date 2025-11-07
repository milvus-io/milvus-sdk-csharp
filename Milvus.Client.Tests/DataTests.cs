using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class DataTests : IClassFixture<DataTests.DataCollectionFixture>, IAsyncLifetime
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
        // Starting with Milvus 2.3.2, Delete no longer seems to return the deleted IDs
        // Assert.Collection(mutationResult.Ids.LongIds!, i => Assert.Equal(2, i));
        Assert.Equal(1, mutationResult.DeleteCount);
        Assert.Equal(0, mutationResult.InsertCount);
        Assert.Equal(0, mutationResult.UpsertCount);

        results = await Collection.QueryAsync(
            "id in [2]",
            new() { ConsistencyLevel = ConsistencyLevel.Strong });
        result = Assert.IsType<FieldData<long>>(Assert.Single(results));
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task Upsert()
    {
        await Collection.InsertAsync(
            [
                FieldData.Create("id", new[] { 1L }),
                FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[] { new[] { 20f, 30f } })
            ]);

        MutationResult upsertResult = await Collection.UpsertAsync(
        [
            FieldData.Create("id", new[] { 1L, 2L }),
            FieldData.CreateFloatVector(
                "float_vector",
                new ReadOnlyMemory<float>[] { new[] { 1f, 2f }, new[] { 3f, 4f } })
        ]);

        Assert.Collection(upsertResult.Ids.LongIds!,
            i => Assert.Equal(1, i),
            i => Assert.Equal(2, i));
        // TODO: Weirdly these all seem to contain 2, though we're supposed to have inserted one row and updated one
        // Assert.Equal(0, upsertResult.DeleteCount);
        // Assert.Equal(1, upsertResult.InsertCount);
        // Assert.Equal(1, upsertResult.UpsertCount);

        IReadOnlyList<FieldData> results = await Collection.QueryAsync(
            "id in [1,2]",
            new()
            {
                OutputFields = { "float_vector" },
                ConsistencyLevel = ConsistencyLevel.Strong
            });

        Assert.Collection(
            results.OrderBy(r => r.FieldName),
            r =>
            {
                Assert.Equal("float_vector", r.FieldName);
                Assert.Collection(
                    ((FloatVectorFieldData)r).Data,
                    v => Assert.Equal(new[] { 1f, 2f }, v),
                    v => Assert.Equal(new[] { 3f, 4f }, v));
            },
            r =>
            {
                Assert.Equal("id", r.FieldName);
                Assert.Equivalent(new[] { 1L, 2L }, ((FieldData<long>)r).Data);
            });
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
        // Any insertion after a flush operation results in generating new segments.
        await InsertDataAsync(5, 6);
        // Wait to avoid rate limiting.
        await Task.Delay(TimeSpan.FromSeconds(12));
        FlushResult newResult = await Collection.FlushAsync();

        Assert.NotEmpty(newResult.CollSegIDs);
        Assert.Equal(CollectionName, newResult.CollSegIDs.First().Key);
        Assert.NotEmpty(newResult.CollSegIDs.First().Value);

        // Wait before next flush call
        await Task.Delay(TimeSpan.FromSeconds(12));
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
        await InsertDataAsync(9, 10);

        // Wait to avoid rate limiting.
        await Task.Delay(TimeSpan.FromSeconds(12));

        // Flush all
        ulong timestamp = await Client.FlushAllAsync();

        // Test if it is a timestamp
        DateTime flushAllDateTime = MilvusTimestampUtils.ToDateTime(timestamp);
        Assert.True(flushAllDateTime - DateTime.UtcNow < TimeSpan.FromSeconds(1));

        // Wait
        await Client.WaitForFlushAllAsync(timestamp);

        IEnumerable<PersistentSegmentInfo> segmentInfos = await Collection.GetPersistentSegmentInfosAsync();
        Assert.True(segmentInfos.All(p => p.State is SegmentState.Flushed or SegmentState.Sealed));
    }

    [Fact]
    public async Task Insert_dynamic_field()
    {
        await Collection.DropAsync();

        await Client.CreateCollectionAsync(
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
        private readonly MilvusClient Client;

        public DataCollectionFixture(MilvusFixture milvusFixture)
        {
            Client = milvusFixture.CreateClient();
            Collection = Client.GetCollection(CollectionName);
        }

        public MilvusCollection Collection;

        public async Task InitializeAsync()
        {
            await Collection.DropAsync();

            await Client.CreateCollectionAsync(
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
        {
            Client.Dispose();
            return Task.CompletedTask;
        }
    }

    private readonly DataCollectionFixture _dataCollectionFixture;
    private const string CollectionName = nameof(DataTests);
    private MilvusCollection Collection => _dataCollectionFixture.Collection;
    private readonly MilvusClient Client;

    public DataTests(MilvusFixture milvusFixture, DataCollectionFixture dataCollectionFixture)
    {
        Client = milvusFixture.CreateClient();
        _dataCollectionFixture = dataCollectionFixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}
