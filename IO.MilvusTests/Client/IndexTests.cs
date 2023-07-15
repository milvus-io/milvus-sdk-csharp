using IO.Milvus;
using IO.Milvus.Client;
using IO.Milvus.Diagnostics;
using Xunit;

namespace IO.MilvusTests.Client;

public class IndexTests
{
    [Fact]
    public async Task Create_vector()
    {
        string collectionName = await CreateCollection();

        await Client.CreateIndexAsync(
            collectionName, "float_vector", "float_vector_idx", MilvusIndexType.Flat, MilvusMetricType.L2, new Dictionary<string, string>());

        // TODO: Consider adding a more idiomatic API here (e.g. have CheckIndexAsync only return when the index has
        // been fully created, expose IProgress and poll internally?

        // TODO: Add rows to exercise this  better
        await Client.WaitForIndexBuildAsync(collectionName, "float_vector");
    }

    [Fact]
    public async Task Create_vector_with_param()
    {
        string collectionName = await CreateCollection();

        await Client.CreateIndexAsync(
            collectionName, "float_vector", "float_vector_idx", MilvusIndexType.Flat, MilvusMetricType.L2,
            // TODO: Consider making the parameter Dictionary instead of IDictionary for target-typed new
            // TODO: Should it be Dictionary<string, object>?
            extraParams: new Dictionary<string, string>
            {
                ["nlist"] = "1024"
            });

        // TODO: Add rows to exercise this  better
        await Client.WaitForIndexBuildAsync(collectionName, "float_vector");
    }

    // TODO: Create scalar index; the API wrapper currently requires index type/metrics which are specific to vectors;
    // expose API for creating scalar indexes.

    [Fact]
    public async Task GetState()
    {
        string collectionName = await CreateCollection();

        Assert.Equal(IndexState.None, await Client.GetIndexStateAsync(collectionName, "float_vector"));

        await Client.CreateIndexAsync(
            collectionName, "float_vector", "float_vector_idx", MilvusIndexType.Flat, MilvusMetricType.L2, new Dictionary<string, string>());
        await Client.WaitForIndexBuildAsync(collectionName, "float_vector");

        Assert.Equal(IndexState.Finished, await Client.GetIndexStateAsync(collectionName, "float_vector"));
    }

    [Fact]
    public async Task GetBuildProgress()
    {
        string collectionName = await CreateCollection();

        await Assert.ThrowsAsync<MilvusException>(() =>
            Client.GetIndexBuildProgressAsync(collectionName, "float_vector"));

        await Client.CreateIndexAsync(
            collectionName, "float_vector", "float_vector_idx", MilvusIndexType.Flat, MilvusMetricType.L2,
            new Dictionary<string, string>());
        await Client.WaitForIndexBuildAsync(collectionName, "float_vector");

        var progress = await Client.GetIndexBuildProgressAsync(collectionName, "float_vector");
        Assert.Equal(progress.TotalRows, progress.IndexedRows);
    }

    [Fact]
    public async Task Describe()
    {
        string collectionName = await CreateCollection();

        await Assert.ThrowsAsync<MilvusException>(() =>
            Client.DescribeIndexAsync(collectionName, "float_vector"));

        await Client.CreateIndexAsync(
            collectionName, "float_vector", "float_vector_idx", MilvusIndexType.Flat, MilvusMetricType.L2,
            extraParams: new Dictionary<string, string>
            {
                ["nlist"] = "1024"
            });
        await Client.WaitForIndexBuildAsync(collectionName, "float_vector");

        var indexes = await Client.DescribeIndexAsync(collectionName, "float_vector");
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
        string collectionName = await CreateCollection();
        await Client.CreateIndexAsync(
            collectionName, "float_vector", "float_vector_idx", MilvusIndexType.Flat, MilvusMetricType.L2,
            new Dictionary<string, string>());
        await Client.WaitForIndexBuildAsync(collectionName, "float_vector");

        await Client.DropIndexAsync(collectionName, "float_vector", "float_vector_idx");

        Assert.Equal(IndexState.None, await Client.GetIndexStateAsync(collectionName, "float_vector"));
    }

    private async Task<string> CreateCollection()
    {
        await Client.DropCollectionAsync(nameof(IndexTests));
        await Client.CreateCollectionAsync(
            nameof(IndexTests),
            new[]
            {
                FieldType.Create<long>("id", isPrimaryKey: true),
                FieldType.CreateVarchar("varchar", 256),
                FieldType.CreateFloatVector("float_vector", 1)
            });

        return nameof(IndexTests);
    }

    private MilvusClient Client => TestEnvironment.Client;
}
