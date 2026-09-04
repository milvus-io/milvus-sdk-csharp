using System.Text.Json;
using Xunit;

namespace Milvus.Client.Tests;

public class IndexTests : IAsyncLifetime
{
    [Fact]
    public async Task Create_vector_index()
    {
        await Collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_vector_index_with_name()
    {
        await Collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2, indexName: "float_vector_idx", cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("float_vector", "float_vector_idx", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_vector_index_with_param()
    {
        await Collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2,
            extraParams: new Dictionary<string, string>
            {
                ["nlist"] = "1024"
            }, cancellationToken: TestContext.Current.CancellationToken);

        await Collection.WaitForIndexBuildAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_scalar_index()
    {
        await Collection.CreateIndexAsync("varchar", cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("varchar", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(IndexType.Flat, """{ "nlist": "8" }""")]
    [InlineData(IndexType.IvfFlat, """{ "nlist": "8" }""")]
    [InlineData(IndexType.IvfSq8, """{ "nlist": "8" }""")]
    [InlineData(IndexType.IvfPq, """{ "nlist": "8", "m": "4" }""")]
    [InlineData(IndexType.Hnsw, """{ "efConstruction": "8", "M": "4" }""")]
    [InlineData(IndexType.Scann, """{ "nlist": "8" }""")]
    [InlineData(IndexType.DiskANN, """{ "nlist": "8" }""")]
    [InlineData(IndexType.AutoIndex, """{ }""")]
    public async Task Index_types_float(IndexType indexType, string extraParamsString)
    {
        await Collection.CreateIndexAsync(
            "float_vector", indexType, SimilarityMetricType.L2,
            extraParams: JsonSerializer.Deserialize<Dictionary<string, string>>(extraParamsString), cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(IndexType.Flat, """{ "nlist": "8" }""")]
    [InlineData(IndexType.IvfFlat, """{ "nlist": "8" }""")]
    [InlineData(IndexType.IvfSq8, """{ "nlist": "8" }""")]
    [InlineData(IndexType.IvfPq, """{ "nlist": "8", "m": "4" }""")]
    [InlineData(IndexType.Hnsw, """{ "efConstruction": "8", "M": "4" }""")]
    [InlineData(IndexType.DiskANN, """{ "nlist": "8" }""")]
    [InlineData(IndexType.AutoIndex, """{ }""")]
    public async Task Index_types_float16(IndexType indexType, string extraParamsString)
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        await Collection.DropAsync(TestContext.Current.CancellationToken);
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateFloat16Vector("float16_vector", 4),
            }, cancellationToken: TestContext.Current.CancellationToken);

        await Collection.CreateIndexAsync("float16_vector", indexType, SimilarityMetricType.L2,
            extraParams: JsonSerializer.Deserialize<Dictionary<string, string>>(extraParamsString), cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("float16_vector", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(IndexType.GpuCagra, """{ "nlist": "8" }""")]
    [InlineData(IndexType.GpuIvfFlat, """{ "nlist": "8" }""")]
    [InlineData(IndexType.GpuIvfPq, """{ "nlist": "8", "m": "4" }""")]
    [InlineData(IndexType.GpuBruteForce, """{ "nlist": "8" }""")]
    public async Task Index_types_float_gpu(IndexType indexType, string extraParamsString)
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            // GPU indexes were introduced in Milvus 2.4
            return;
        }

        try
        {
            await Collection.CreateIndexAsync(
                "float_vector", indexType, SimilarityMetricType.L2,
                extraParams: JsonSerializer.Deserialize<Dictionary<string, string>>(extraParamsString), cancellationToken: TestContext.Current.CancellationToken);
            await Collection.WaitForIndexBuildAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken);
        }
        catch (MilvusException ex) when (ex.Message.Contains("invalid index type", StringComparison.Ordinal))
        {
            // Skip test if GPU support is not available in the test environment.
        }
    }

    [Theory]
    [InlineData(IndexType.BinFlat, """{ "n_trees": "10" }""")]
    [InlineData(IndexType.BinIvfFlat, """{ "n_trees": "8", "nlist": "8" }""")]
    public async Task Index_types_binary(IndexType indexType, string extraParamsString)
    {
        await Collection.DropAsync(TestContext.Current.CancellationToken);
        await Client.CreateCollectionAsync(
            nameof(IndexTests),
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateBinaryVector("binary_vector", 8),
            }, cancellationToken: TestContext.Current.CancellationToken);

        await Collection.CreateIndexAsync(
            "binary_vector", indexType, SimilarityMetricType.Jaccard,
            extraParams: JsonSerializer.Deserialize<Dictionary<string, string>>(extraParamsString), cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("binary_vector", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(SimilarityMetricType.L2)]
    [InlineData(SimilarityMetricType.Ip)]
    [InlineData(SimilarityMetricType.Cosine)]
    public async Task Similarity_metric_types(SimilarityMetricType similarityMetricType)
    {
        await Collection.CreateIndexAsync("float_vector", IndexType.Flat, similarityMetricType, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(SimilarityMetricType.Jaccard)]
    [InlineData(SimilarityMetricType.Hamming)]
    public async Task Similarity_metric_types_binary(SimilarityMetricType similarityMetricType)
    {
        await Collection.DropAsync(TestContext.Current.CancellationToken);
        await Client.CreateCollectionAsync(
            nameof(IndexTests),
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateBinaryVector("binary_vector", 8),
            }, cancellationToken: TestContext.Current.CancellationToken);

        await Collection.CreateIndexAsync("binary_vector", IndexType.BinFlat, similarityMetricType, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("binary_vector", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Scalar_index_inverted()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        await Collection.CreateIndexAsync("varchar", IndexType.Inverted, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("varchar", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Scalar_index_trie()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        await Collection.CreateIndexAsync("varchar", IndexType.Trie, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("varchar", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Scalar_index_stl_sort()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        await Collection.DropAsync(TestContext.Current.CancellationToken);
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.Create<long>("numeric_field"),
                FieldSchema.CreateFloatVector("float_vector", 4),
            }, cancellationToken: TestContext.Current.CancellationToken);

        await Collection.CreateIndexAsync("numeric_field", IndexType.StlSort, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("numeric_field", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Scalar_index_bitmap()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 5))
        {
            return;
        }

        await Collection.CreateIndexAsync("varchar", IndexType.Bitmap, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("varchar", cancellationToken: TestContext.Current.CancellationToken);

        await Collection.InsertAsync(new FieldData[]
        {
            FieldData.Create("id", new long[] { 1, 2, 3 }),
            FieldData.Create("varchar", new[] { "a", "b", "a" }),
            FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
            {
                new float[] { 1, 1, 1, 1 }, new float[] { 2, 2, 2, 2 }, new float[] { 3, 3, 3, 3 },
            }),
        }, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForCollectionLoadAsync(cancellationToken: TestContext.Current.CancellationToken);

        var results = await Collection.QueryAsync(
            "varchar == \"a\"", new QueryParameters { OutputFields = { "id" } },
            cancellationToken: TestContext.Current.CancellationToken);
        var idData = (FieldData<long>)Assert.Single(results, f => f.FieldName == "id");
        Assert.Equal(new long[] { 1, 3 }, idData.Data);
    }

    [Fact]
    public async Task Ngram_index()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 6))
        {
            return;
        }

        await Collection.CreateIndexAsync(
            "varchar", IndexType.Ngram,
            extraParams: new Dictionary<string, string> { ["min_gram"] = "2", ["max_gram"] = "3" },
            cancellationToken: TestContext.Current.CancellationToken);
        await Collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("varchar", cancellationToken: TestContext.Current.CancellationToken);

        await Collection.InsertAsync(new FieldData[]
        {
            FieldData.Create("id", new long[] { 1, 2, 3 }),
            FieldData.Create("varchar", new[] { "hello world", "foobar", "abcdef" }),
            FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
            {
                new float[] { 1, 1, 1, 1 }, new float[] { 2, 2, 2, 2 }, new float[] { 3, 3, 3, 3 },
            }),
        }, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForCollectionLoadAsync(cancellationToken: TestContext.Current.CancellationToken);

        var results = await Collection.QueryAsync(
            "varchar like \"%bcd%\"", new QueryParameters { OutputFields = { "id" } },
            cancellationToken: TestContext.Current.CancellationToken);
        var idData = (FieldData<long>)Assert.Single(results, f => f.FieldName == "id");
        Assert.Equal(new long[] { 3 }, idData.Data);
    }

    [Fact]
    public async Task Sparse_inverted_index()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        await Collection.DropAsync(TestContext.Current.CancellationToken);
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateSparseFloatVector("sparse_vector"),
            }, cancellationToken: TestContext.Current.CancellationToken);

        await Collection.CreateIndexAsync(
            "sparse_vector",
            IndexType.SparseInvertedIndex,
            SimilarityMetricType.Ip,
            extraParams: new Dictionary<string, string>
            {
                ["drop_ratio_build"] = "0.2"
            }, cancellationToken: TestContext.Current.CancellationToken);

        await Collection.WaitForIndexBuildAsync("sparse_vector", cancellationToken: TestContext.Current.CancellationToken);

        var indexes = await Collection.DescribeIndexAsync("sparse_vector", cancellationToken: TestContext.Current.CancellationToken);
        var index = Assert.Single(indexes);
        Assert.Contains(index.Params, kv => kv is { Key: "index_type", Value: "SPARSE_INVERTED_INDEX" });
    }

    [Fact]
    public async Task IvfRabitq_index()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 6))
        {
            return;
        }

        await Collection.CreateIndexAsync(
            "float_vector", IndexType.IvfRabitq, SimilarityMetricType.L2,
            extraParams: new Dictionary<string, string> { ["nlist"] = "8" },
            cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken);

        ReadOnlyMemory<float>[] vectors = { new float[] { 1, 1, 1, 1 }, new float[] { 2, 2, 2, 2 } };
        await Collection.InsertAsync(new FieldData[]
        {
            FieldData.Create("id", new long[] { 1, 2 }),
            FieldData.Create("varchar", new[] { "one", "two" }),
            FieldData.CreateFloatVector("float_vector", vectors),
        }, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForCollectionLoadAsync(cancellationToken: TestContext.Current.CancellationToken);

        var results = await Collection.SearchAsync(
            "float_vector", new[] { vectors[0] }, SimilarityMetricType.L2, limit: 1,
            parameters: new SearchParameters { ExtraParameters = { ["nprobe"] = "8" } },
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, Assert.Single(results.Ids.LongIds!));
    }

    [Fact]
    public async Task MinHashLsh_index()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 6))
        {
            return;
        }

        await Collection.DropAsync(TestContext.Current.CancellationToken);
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                // Dimension = mh_element_bit_width (64) * number of MinHash signatures per row (4) = 256 bits.
                FieldSchema.CreateBinaryVector("minhash_signature", 256),
            }, cancellationToken: TestContext.Current.CancellationToken);

        await Collection.CreateIndexAsync(
            "minhash_signature", IndexType.MinHashLsh, SimilarityMetricType.MhJaccard,
            extraParams: new Dictionary<string, string>
            {
                ["mh_element_bit_width"] = "64",
                ["mh_lsh_band"] = "4",
            }, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("minhash_signature", cancellationToken: TestContext.Current.CancellationToken);

        ReadOnlyMemory<byte>[] vectors =
        {
            Enumerable.Range(1, 32).Select(i => (byte)i).ToArray(),
            Enumerable.Range(50, 32).Select(i => (byte)i).ToArray(),
        };
        await Collection.InsertAsync(new FieldData[]
        {
            FieldData.Create("id", new long[] { 1, 2 }),
            FieldData.CreateBinaryVectors("minhash_signature", vectors),
        }, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForCollectionLoadAsync(cancellationToken: TestContext.Current.CancellationToken);

        var results = await Collection.SearchAsync(
            "minhash_signature", new[] { vectors[0] }, SimilarityMetricType.MhJaccard, limit: 2,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(1L, results.Ids.LongIds!);
    }

#pragma warning disable CS0618 // Type or member is obsolete
    [Fact]
    public async Task GetState()
    {
        try
        {
            Assert.Equal(IndexState.None, await Collection.GetIndexStateAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken));
        }
        catch (MilvusException e) when (e.Message.Contains("IndexNotFound", StringComparison.Ordinal))
        {
            // In recent versions of Milvus, querying state of non-existent index throws an error.
        }

        await Collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(IndexState.Finished, await Collection.GetIndexStateAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetBuildProgress_with_name()
    {
        await Assert.ThrowsAsync<MilvusException>(() =>
            Collection.GetIndexBuildProgressAsync("float_vector", indexName: "float_vector_idx", cancellationToken: TestContext.Current.CancellationToken));

        await Collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2, indexName: "float_vector_idx", cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("float_vector", "float_vector_idx", cancellationToken: TestContext.Current.CancellationToken);

        var progress = await Collection.GetIndexBuildProgressAsync("float_vector", "float_vector_idx", TestContext.Current.CancellationToken);
        Assert.Equal(progress.TotalRows, progress.IndexedRows);
    }
#pragma warning restore CS0618 // Type or member is obsolete

    [Fact]
    public async Task Describe()
    {
        await Assert.ThrowsAsync<MilvusException>(() => Collection.DescribeIndexAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken));

        await Collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2,
            indexName: "float_vector_idx", extraParams: new Dictionary<string, string>
            {
                ["nlist"] = "1024"
            }, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken);

        var indexes = await Collection.DescribeIndexAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken);
        var index = Assert.Single(indexes);

        Assert.Equal("float_vector_idx", index.IndexName);
        Assert.Equal("float_vector", index.FieldName);
        var parameters = index.Params;

        Assert.Contains(parameters, kv => kv is { Key: "index_type", Value: "FLAT" });
        Assert.Contains(parameters, kv => kv is { Key: "metric_type", Value: "L2" });

        // TODO: Look into making this a nice structured dictionary rather than a serialized string
        Assert.Equal("""{"nlist":1024}""", parameters["params"]);
    }

    [Fact]
    public async Task Drop()
    {
        await Collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2, indexName: "float_vector_idx", cancellationToken: TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken);

        await Collection.DropIndexAsync("float_vector", "float_vector_idx", TestContext.Current.CancellationToken);

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(
            () => Collection.DescribeIndexAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(MilvusErrorCode.IndexNotFound, exception.ErrorCode);
    }

    public async ValueTask InitializeAsync()
    {
        await Collection.DropAsync();
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateFloatVector("float_vector", 4),
            });
    }

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }

    private const string CollectionName = nameof(IndexTests);

    private readonly MilvusClient Client;

    private MilvusCollection Collection { get; }

    public IndexTests(MilvusFixture milvusFixture)
    {
        Client = milvusFixture.CreateClient();
        Collection = Client.GetCollection(CollectionName);
    }
}
