using System.Text.Json;
using Xunit;

namespace Milvus.Client.Tests;

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
    [InlineData(IndexType.AutoIndex, """{ }""")]
    public async Task Index_types_float(IndexType indexType, string extraParamsString)
    {
        await Collection.CreateIndexAsync(
            "float_vector", indexType, SimilarityMetricType.L2,
            extraParams: JsonSerializer.Deserialize<Dictionary<string, string>>(extraParamsString));
        await Collection.WaitForIndexBuildAsync("float_vector");
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

#pragma warning disable CS0618 // Type or member is obsolete
    [Fact]
    public async Task GetState()
    {
        Assert.Equal(IndexState.None, await Collection.GetIndexStateAsync("float_vector"));

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
        Assert.Equal(MilvusErrorCode.IndexNotExist, exception.ErrorCode);
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
        => Task.CompletedTask;

    private const string CollectionName = nameof(IndexTests);

    private MilvusClient Client => TestEnvironment.Client;

    private MilvusCollection Collection { get; }

    public IndexTests()
        => Collection = Client.GetCollection(CollectionName);
}
