using System.Text.Json;
using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class IndexTests : IAsyncLifetime
{
    [Fact]
    public async Task Create_vector_index()
    {
        await Collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);
        await Collection.WaitForIndexBuildAsync("float_vector");
    }

    [Fact]
    public async Task Create_vector_index_with_name()
    {
        await Collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2, indexName: "float_vector_idx");
        await Collection.WaitForIndexBuildAsync("float_vector", "float_vector_idx");
    }

    [Fact]
    public async Task Create_vector_index_with_param()
    {
        await Collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2,
            extraParams: new Dictionary<string, string>
            {
                ["nlist"] = "1024"
            });

        await Collection.WaitForIndexBuildAsync("float_vector");
    }

    [Fact]
    public async Task Create_scalar_index()
    {
        await Collection.CreateIndexAsync("varchar");
        await Collection.WaitForIndexBuildAsync("varchar");
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
            extraParams: JsonSerializer.Deserialize<Dictionary<string, string>>(extraParamsString));
        await Collection.WaitForIndexBuildAsync("float_vector");
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

        await Collection.DropAsync();
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateFloat16Vector("float16_vector", 4),
            });

        await Collection.CreateIndexAsync("float16_vector", indexType, SimilarityMetricType.L2,
            extraParams: JsonSerializer.Deserialize<Dictionary<string, string>>(extraParamsString));
        await Collection.WaitForIndexBuildAsync("float16_vector");
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
                extraParams: JsonSerializer.Deserialize<Dictionary<string, string>>(extraParamsString));
            await Collection.WaitForIndexBuildAsync("float_vector");
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
        await Collection.DropAsync();
        await Client.CreateCollectionAsync(
            nameof(IndexTests),
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateBinaryVector("binary_vector", 8),
            });

        await Collection.CreateIndexAsync(
            "binary_vector", indexType, SimilarityMetricType.Jaccard,
            extraParams: JsonSerializer.Deserialize<Dictionary<string, string>>(extraParamsString));
        await Collection.WaitForIndexBuildAsync("binary_vector");
    }

    [Theory]
    [InlineData(SimilarityMetricType.L2)]
    [InlineData(SimilarityMetricType.Ip)]
    [InlineData(SimilarityMetricType.Cosine)]
    public async Task Similarity_metric_types(SimilarityMetricType similarityMetricType)
    {
        await Collection.CreateIndexAsync("float_vector", IndexType.Flat, similarityMetricType);
        await Collection.WaitForIndexBuildAsync("float_vector");
    }

    [Theory]
    [InlineData(SimilarityMetricType.Jaccard)]
    [InlineData(SimilarityMetricType.Hamming)]
    public async Task Similarity_metric_types_binary(SimilarityMetricType similarityMetricType)
    {
        await Collection.DropAsync();
        await Client.CreateCollectionAsync(
            nameof(IndexTests),
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateBinaryVector("binary_vector", 8),
            });

        await Collection.CreateIndexAsync("binary_vector", IndexType.BinFlat, similarityMetricType);
        await Collection.WaitForIndexBuildAsync("binary_vector");
    }

    [Fact]
    public async Task Scalar_index_inverted()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        await Collection.CreateIndexAsync("varchar", IndexType.Inverted);
        await Collection.WaitForIndexBuildAsync("varchar");
    }

    [Fact]
    public async Task Scalar_index_trie()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        await Collection.CreateIndexAsync("varchar", IndexType.Trie);
        await Collection.WaitForIndexBuildAsync("varchar");
    }

    [Fact]
    public async Task Scalar_index_stl_sort()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        await Collection.DropAsync();
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.Create<long>("numeric_field"),
                FieldSchema.CreateFloatVector("float_vector", 4),
            });

        await Collection.CreateIndexAsync("numeric_field", IndexType.StlSort);
        await Collection.WaitForIndexBuildAsync("numeric_field");
    }

    [Fact]
    public async Task Sparse_inverted_index()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        await Collection.DropAsync();
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateSparseFloatVector("sparse_vector"),
            });

        await Collection.CreateIndexAsync(
            "sparse_vector",
            IndexType.SparseInvertedIndex,
            SimilarityMetricType.Ip,
            extraParams: new Dictionary<string, string>
            {
                ["drop_ratio_build"] = "0.2"
            });

        await Collection.WaitForIndexBuildAsync("sparse_vector");

        var indexes = await Collection.DescribeIndexAsync("sparse_vector");
        var index = Assert.Single(indexes);
        Assert.Contains(index.Params, kv => kv is { Key: "index_type", Value: "SPARSE_INVERTED_INDEX" });
    }

#pragma warning disable CS0618 // Type or member is obsolete
    [Fact]
    public async Task GetState()
    {
        try
        {
            Assert.Equal(IndexState.None, await Collection.GetIndexStateAsync("float_vector"));
        }
        catch (MilvusException e) when (e.Message.Contains("IndexNotFound", StringComparison.Ordinal))
        {
            // In recent versions of Milvus, querying state of non-existent index throws an error.
        }

        await Collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);
        await Collection.WaitForIndexBuildAsync("float_vector");

        Assert.Equal(IndexState.Finished, await Collection.GetIndexStateAsync("float_vector"));
    }

    [Fact]
    public async Task GetBuildProgress_with_name()
    {
        await Assert.ThrowsAsync<MilvusException>(() =>
            Collection.GetIndexBuildProgressAsync("float_vector", indexName: "float_vector_idx"));

        await Collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2, indexName: "float_vector_idx");
        await Collection.WaitForIndexBuildAsync("float_vector", "float_vector_idx");

        var progress = await Collection.GetIndexBuildProgressAsync("float_vector", "float_vector_idx");
        Assert.Equal(progress.TotalRows, progress.IndexedRows);
    }
#pragma warning restore CS0618 // Type or member is obsolete

    [Fact]
    public async Task Describe()
    {
        await Assert.ThrowsAsync<MilvusException>(() => Collection.DescribeIndexAsync("float_vector"));

        await Collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2,
            indexName: "float_vector_idx", extraParams: new Dictionary<string, string>
            {
                ["nlist"] = "1024"
            });
        await Collection.WaitForIndexBuildAsync("float_vector");

        var indexes = await Collection.DescribeIndexAsync("float_vector");
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
            "float_vector", IndexType.Flat, SimilarityMetricType.L2, indexName: "float_vector_idx");
        await Collection.WaitForIndexBuildAsync("float_vector");

        await Collection.DropIndexAsync("float_vector", "float_vector_idx");

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(
            () => Collection.DescribeIndexAsync("float_vector"));
        Assert.Equal(MilvusErrorCode.IndexNotFound, exception.ErrorCode);
    }

    public async Task InitializeAsync()
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

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
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
