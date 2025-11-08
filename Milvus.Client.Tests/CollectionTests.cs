using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class CollectionTests : IAsyncLifetime
{
    [Fact]
    public async Task Create_Exists_and_Drop()
    {
        MilvusCollection collection = await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("vector", dimension: 2)
            });
        Assert.True(await Client.HasCollectionAsync(CollectionName));

        await collection.DropAsync();
        Assert.False(await Client.HasCollectionAsync(CollectionName));
    }

    [Fact]
    public async Task Describe()
    {
        await Assert.ThrowsAsync<MilvusException>(() => Client.GetCollection(CollectionName).DescribeAsync());

        var collection = await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("book_id", isPrimaryKey: true, autoId: true),
                FieldSchema.Create<bool>("is_cartoon", description: "Some cartoon"),
                FieldSchema.Create<sbyte>("chapter_count"),
                FieldSchema.Create<short>("short_page_count"),
                FieldSchema.Create<int>("int32_page_count"),
                FieldSchema.Create<long>("word_count"),
                FieldSchema.Create<float>("float_weight"),
                FieldSchema.Create<double>("double_weight"),
                FieldSchema.CreateVarchar("book_name", maxLength: 256, isPartitionKey: true),
                FieldSchema.CreateFloatVector("book_intro", dimension: 2),
                FieldSchema.CreateJson("some_json")
            },
            shardsNum: 2,
            consistencyLevel: ConsistencyLevel.Eventually);

        var collectionDescription = await collection.DescribeAsync();

        Assert.Equal(CollectionName, collectionDescription.CollectionName);
        Assert.Equal(2, collectionDescription.ShardsNum);
        Assert.Equal(ConsistencyLevel.Eventually, collectionDescription.ConsistencyLevel);

        Assert.All(collectionDescription.Schema.Fields, f => Assert.Equal(FieldState.FieldCreated, f.State));

        Assert.Collection(collectionDescription.Schema.Fields,
            f =>
            {
                Assert.Equal("book_id", f.Name);
                Assert.Equal(MilvusDataType.Int64, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.True(f.IsPrimaryKey);
                Assert.True(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("is_cartoon", f.Name);
                Assert.Equal(MilvusDataType.Bool, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("Some cartoon", f.Description);
            },
            f =>
            {
                Assert.Equal("chapter_count", f.Name);
                Assert.Equal(MilvusDataType.Int8, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("short_page_count", f.Name);
                Assert.Equal(MilvusDataType.Int16, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("int32_page_count", f.Name);
                Assert.Equal(MilvusDataType.Int32, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("word_count", f.Name);
                Assert.Equal(MilvusDataType.Int64, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("float_weight", f.Name);
                Assert.Equal(MilvusDataType.Float, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("double_weight", f.Name);
                Assert.Equal(MilvusDataType.Double, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("book_name", f.Name);
                Assert.Equal(MilvusDataType.VarChar, f.DataType);
                Assert.Equal(256, f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.True(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("book_intro", f.Name);
                Assert.Equal(MilvusDataType.FloatVector, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Equal(2, f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("some_json", f.Name);
                Assert.Equal(MilvusDataType.Json, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            });
    }

    [Fact]
    public async Task Rename()
    {
        string renamedCollectionName = "RenamedCollection";

        await Client.GetCollection(renamedCollectionName).DropAsync();

        var collection = await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("vector", dimension: 2)
            });

        await collection.RenameAsync(renamedCollectionName);

        Assert.False(await Client.HasCollectionAsync(CollectionName));
        Assert.True(await Client.HasCollectionAsync(renamedCollectionName));

        Assert.Equal(renamedCollectionName, (await collection.DescribeAsync()).CollectionName);
    }

    [Fact]
    public async Task Load_Release()
    {
        var collection = await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });

        await collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2, "float_vector_idx", new Dictionary<string, string>());

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        _ = await collection.QueryAsync("id in [2, 3]", new() { OutputFields = { "float_vector" } });

        await collection.ReleaseAsync();

        await Assert.ThrowsAsync<MilvusException>(() =>
            collection.QueryAsync("id in [2, 3]", new() { OutputFields = { "float_vector" } }));
    }

    [Fact]
    public async Task List()
    {
        var collection = await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });

        await collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2, "float_vector_idx", new Dictionary<string, string>());

        Assert.Single(await Client.ListCollectionsAsync(), c => c.Name == CollectionName);
        Assert.DoesNotContain(await Client.ListCollectionsAsync(filter: CollectionFilter.InMemory),
            c => c.Name == CollectionName);

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        Assert.Single(await Client.ListCollectionsAsync(), c => c.Name == CollectionName);
        Assert.Single(await Client.ListCollectionsAsync(filter: CollectionFilter.InMemory),
            c => c.Name == CollectionName);
    }

    [Fact]
    public async Task GetEntityCount()
    {
        var collection = await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });

        Assert.Equal(0, await collection.GetEntityCountAsync());

        await collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new long[] { 1, 2 }),
                FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
                {
                    new[] { 1f, 2f },
                    new[] { 3f, 4f }
                })
            });

        await collection.FlushAsync();

        // There's some delay in updating the statistics so we only assert the existence of row_count for now
        _ = await collection.GetEntityCountAsync();
    }

    [Fact]
    public async Task Compact()
    {
        var collection = await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });

        long compactionId = await collection.CompactAsync();
        if (await Client.GetParsedMilvusVersion() >= new Version(2, 4))
        {
            // Milvus 2.4 returns -1 here as the compaction ID
            return;
        }

        Assert.NotEqual(0, compactionId);
        await Client.WaitForCompactionAsync(compactionId);

        CompactionState state = await Client.GetCompactionStateAsync(compactionId);
        Assert.Equal(CompactionState.Completed, state);

        CompactionPlans compactionPlans = await Client.GetCompactionPlansAsync(compactionId);
        Assert.Equal(CompactionState.Completed, compactionPlans.State);
    }

    [Fact]
    public async Task Collection_with_multiple_float_vector_fields()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        var collection = await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("text", 512),
                FieldSchema.CreateFloatVector("embedding_small", 128),
                FieldSchema.CreateFloatVector("embedding_large", 768),
            });

        var idData = FieldData.Create("id", new[] { 1L, 2L, 3L });
        var textData = FieldData.CreateVarChar("text", new[] { "doc1", "doc2", "doc3" });

        var embeddingSmallData = FieldData.CreateFloatVector("embedding_small", new ReadOnlyMemory<float>[]
        {
            Enumerable.Range(0, 128).Select(i => i / 128f).ToArray(),
            Enumerable.Range(0, 128).Select(i => (i + 1) / 128f).ToArray(),
            Enumerable.Range(0, 128).Select(i => (i + 2) / 128f).ToArray()
        });

        var embeddingLargeData = FieldData.CreateFloatVector("embedding_large", new ReadOnlyMemory<float>[]
        {
            Enumerable.Range(0, 768).Select(i => i / 768f).ToArray(),
            Enumerable.Range(0, 768).Select(i => (i + 1) / 768f).ToArray(),
            Enumerable.Range(0, 768).Select(i => (i + 2) / 768f).ToArray()
        });

        await collection.InsertAsync(new FieldData[] { idData, textData, embeddingSmallData, embeddingLargeData });

        await collection.CreateIndexAsync("embedding_small", IndexType.Flat, SimilarityMetricType.L2);
        await collection.WaitForIndexBuildAsync("embedding_small");

        await collection.CreateIndexAsync("embedding_large", IndexType.Flat, SimilarityMetricType.L2);
        await collection.WaitForIndexBuildAsync("embedding_large");

        await collection.LoadAsync();
    }

    [Fact]
    public async Task Collection_with_float_and_float16_vectors()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        var collection = await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("text", 512),
                FieldSchema.CreateFloatVector("float_vec", 4),
                FieldSchema.CreateFloat16Vector("float16_vec", 4),
            });

        var idData = FieldData.Create("id", new[] { 1L, 2L });
        var textData = FieldData.CreateVarChar("text", new[] { "doc1", "doc2" });

        var floatVecData = FieldData.CreateFloatVector("float_vec", new ReadOnlyMemory<float>[]
        {
            new[] { 1.0f, 2.0f, 3.0f, 4.0f },
            new[] { 5.0f, 6.0f, 7.0f, 8.0f }
        });

        var float16VecData = FieldData.CreateFloat16Vector("float16_vec", new ReadOnlyMemory<Half>[]
        {
            new[] { (Half)1.0f, (Half)2.0f, (Half)3.0f, (Half)4.0f },
            new[] { (Half)5.0f, (Half)6.0f, (Half)7.0f, (Half)8.0f }
        });

        await collection.InsertAsync(new FieldData[] { idData, textData, floatVecData, float16VecData });

        await collection.CreateIndexAsync("float_vec", IndexType.Flat, SimilarityMetricType.L2);
        await collection.WaitForIndexBuildAsync("float_vec");

        await collection.CreateIndexAsync("float16_vec", IndexType.Flat, SimilarityMetricType.L2);
        await collection.WaitForIndexBuildAsync("float16_vec");

        await collection.LoadAsync();
    }

    [Fact]
    public async Task Create_in_non_existing_database_fails()
    {
        // using MilvusClient databaseClient = TestEnvironment.CreateClientForDatabase("non_existing_db");
        using MilvusClient databaseClient = Fixture.CreateClient("non_existing_db");

        var exception = await Assert.ThrowsAsync<MilvusException>(() => databaseClient.CreateCollectionAsync(
            "foo",
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("vector", dimension: 2)
            }));

        Assert.Contains("not found", exception.Message);
        Assert.Contains("non_existing_db", exception.Message);
    }

    public CollectionTests(MilvusFixture milvusFixture)
    {
        ArgumentNullException.ThrowIfNull(milvusFixture);
        Fixture = milvusFixture;
        Client = milvusFixture.CreateClient();
    }

    public Task InitializeAsync()
        => Client.GetCollection(CollectionName).DropAsync();

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    private const string CollectionName = nameof(CollectionTests);

    private readonly MilvusFixture Fixture;
    private readonly MilvusClient Client;
}
