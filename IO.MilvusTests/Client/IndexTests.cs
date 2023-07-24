using System.Text.Json;
using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public class IndexTests : IAsyncLifetime
{
    [Fact]
    public async Task Create_vector_index()
    {
        await Collection.CreateIndexAsync("float_vector", MilvusIndexType.Flat, MilvusSimilarityMetricType.L2);
        await Collection.WaitForIndexBuildAsync("float_vector");
    }

    [Fact]
    public async Task Create_vector_index_with_name()
    {
        await Collection.CreateIndexAsync(
            "float_vector", MilvusIndexType.Flat, MilvusSimilarityMetricType.L2, indexName: "float_vector_idx");
        await Collection.WaitForIndexBuildAsync("float_vector");
    }

    [Fact]
    public async Task Create_vector_index_with_param()
    {
        await Collection.CreateIndexAsync(
            "float_vector", MilvusIndexType.Flat, MilvusSimilarityMetricType.L2,
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
    [InlineData(MilvusIndexType.Flat, """{ "nlist": "8" }""")]
    [InlineData(MilvusIndexType.IvfFlat, """{ "nlist": "8" }""")]
    [InlineData(MilvusIndexType.IvfSq8, """{ "nlist": "8" }""")]
    [InlineData(MilvusIndexType.IvfPq, """{ "nlist": "8", "m": "4" }""")]
    [InlineData(MilvusIndexType.Hnsw, """{ "efConstruction": "8", "M": "4" }""")]
    [InlineData(MilvusIndexType.Annoy, """{ "n_trees": "10" }""")]
    [InlineData(MilvusIndexType.RhnswFlat, """{ "efConstruction": "8", "M": "4" }""")]
    [InlineData(MilvusIndexType.RhnswPq, """{ "efConstruction": "8", "M": "4", "PQM": "4" }""")]
    [InlineData(MilvusIndexType.RhnswSq, """{ "efConstruction": "8", "M": "4" }""")]
    [InlineData(MilvusIndexType.AutoIndex, """{ }""")]
    public async Task Index_types_float(MilvusIndexType indexType, string extraParamsString)
    {
        await Collection.CreateIndexAsync("float_vector", indexType, MilvusSimilarityMetricType.L2,
            JsonSerializer.Deserialize<Dictionary<string, string>>(extraParamsString));
        await Collection.WaitForIndexBuildAsync("float_vector");
    }

    [Theory]
    [InlineData(MilvusIndexType.BinFlat, """{ "n_trees": "10" }""")]
    [InlineData(MilvusIndexType.BinIvfFlat, """{ "n_trees": "8", "nlist": "8" }""")]
    public async Task Index_types_binary(MilvusIndexType indexType, string extraParamsString)
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

        await Collection.CreateIndexAsync("binary_vector", indexType, MilvusSimilarityMetricType.Jaccard,
            JsonSerializer.Deserialize<Dictionary<string, string>>(extraParamsString));
        await Collection.WaitForIndexBuildAsync("binary_vector");
    }

    [Theory]
    [InlineData(MilvusSimilarityMetricType.L2)]
    [InlineData(MilvusSimilarityMetricType.Ip)]
    public async Task Similarity_metric_types(MilvusSimilarityMetricType similarityMetricType)
    {
        await Collection.CreateIndexAsync("float_vector", MilvusIndexType.Flat, similarityMetricType);
        await Collection.WaitForIndexBuildAsync("float_vector");
    }

    [Theory]
    [InlineData(MilvusSimilarityMetricType.Jaccard)]
    [InlineData(MilvusSimilarityMetricType.Tanimoto)]
    [InlineData(MilvusSimilarityMetricType.Hamming)]
    [InlineData(MilvusSimilarityMetricType.Superstructure)]
    [InlineData(MilvusSimilarityMetricType.Substructure)]
    public async Task Similarity_metric_types_binary(MilvusSimilarityMetricType similarityMetricType)
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

        await Collection.CreateIndexAsync("binary_vector", MilvusIndexType.BinFlat, similarityMetricType);
        await Collection.WaitForIndexBuildAsync("binary_vector");
    }

    [Fact]
    public async Task GetState()
    {
        Assert.Equal(IndexState.None, await Collection.GetIndexStateAsync("float_vector"));

        await Collection.CreateIndexAsync("float_vector", MilvusIndexType.Flat, MilvusSimilarityMetricType.L2);
        await Collection.WaitForIndexBuildAsync("float_vector");

        Assert.Equal(IndexState.Finished, await Collection.GetIndexStateAsync("float_vector"));
    }

    [Fact]
    public async Task GetBuildProgress()
    {
        await Assert.ThrowsAsync<MilvusException>(() =>
            Collection.GetIndexBuildProgressAsync("float_vector"));

        await Collection.CreateIndexAsync("float_vector", MilvusIndexType.Flat, MilvusSimilarityMetricType.L2);
        await Collection.WaitForIndexBuildAsync("float_vector");

        var progress = await Collection.GetIndexBuildProgressAsync("float_vector");
        Assert.Equal(progress.TotalRows, progress.IndexedRows);
    }

    [Fact]
    public async Task Describe()
    {
        await Assert.ThrowsAsync<MilvusException>(() => Collection.DescribeIndexAsync("float_vector"));

        await Collection.CreateIndexAsync(
            "float_vector", MilvusIndexType.Flat, MilvusSimilarityMetricType.L2,
            extraParams: new Dictionary<string, string>
            {
                ["nlist"] = "1024"
            },
            indexName: "float_vector_idx");
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
            "float_vector", MilvusIndexType.Flat, MilvusSimilarityMetricType.L2, indexName: "float_vector_idx");
        await Collection.WaitForIndexBuildAsync("float_vector");

        await Collection.DropIndexAsync("float_vector", "float_vector_idx");

        Assert.Equal(IndexState.None, await Collection.GetIndexStateAsync("float_vector"));
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
