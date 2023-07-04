using IO.Milvus;
using IO.Milvus.Client;
using IO.Milvus.Diagnostics;
using Xunit;

namespace IO.MilvusTests.Client;

public class IndexTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Create_vector(IMilvusClient client)
    {
        var collectionName = await CreateCollection(client);

        await client.CreateIndexAsync(
            collectionName, "float_vector", "float_vector_idx", MilvusIndexType.FLAT, MilvusMetricType.L2);

        // TODO: Consider adding a more idiomatic API here (e.g. have CheckIndexAsync only return when the index has
        // been fully created, expose IProgress and poll internally?

        // TODO: Add rows to exercise this  better
        await WaitForIndexBuild(client, collectionName, "float_vector");
    }

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Create_vector_with_param(IMilvusClient client)
    {
        var collectionName = await CreateCollection(client);

        await client.CreateIndexAsync(
            collectionName, "float_vector", "float_vector_idx", MilvusIndexType.FLAT, MilvusMetricType.L2,
            // TODO: Consider making the parameter Dictionary instead of IDictionary for target-typed new
            // TODO: Should it be Dictionary<string, object>?
            extraParams: new Dictionary<string, string>
            {
                ["nlist"] = "1024"
            });

        // TODO: Add rows to exercise this  better
        await WaitForIndexBuild(client, collectionName, "float_vector");
    }

    // TODO: Create scalar index; the API wrapper currently requires index type/metrics which are specific to vectors;
    // expose API for creating scalar indexes.

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task GetState(IMilvusClient client)
    {
        var collectionName = await CreateCollection(client);

        Assert.Equal(IndexState.None, await client.GetIndexStateAsync(collectionName, "float_vector"));

        await client.CreateIndexAsync(
            collectionName, "float_vector", "float_vector_idx", MilvusIndexType.FLAT, MilvusMetricType.L2);
        await WaitForIndexBuild(client, collectionName, "float_vector");

        Assert.Equal(IndexState.Finished, await client.GetIndexStateAsync(collectionName, "float_vector"));
    }

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task GetBuildProgress(IMilvusClient client)
    {
        var collectionName = await CreateCollection(client);

        await Assert.ThrowsAsync<MilvusException>(() =>
            client.GetIndexBuildProgressAsync(collectionName, "float_vector"));

        await client.CreateIndexAsync(
            collectionName, "float_vector", "float_vector_idx", MilvusIndexType.FLAT, MilvusMetricType.L2);
        await WaitForIndexBuild(client, collectionName, "float_vector");

        var progress = await client.GetIndexBuildProgressAsync(collectionName, "float_vector");
        Assert.Equal(progress.TotalRows, progress.IndexedRows);
    }

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Describe(IMilvusClient client)
    {
        var collectionName = await CreateCollection(client);

        await Assert.ThrowsAsync<MilvusException>(() =>
            client.DescribeIndexAsync(collectionName, "float_vector"));

        await client.CreateIndexAsync(
            collectionName, "float_vector", "float_vector_idx", MilvusIndexType.FLAT, MilvusMetricType.L2,
            extraParams: new Dictionary<string, string>
            {
                ["nlist"] = "1024"
            });
        await WaitForIndexBuild(client, collectionName, "float_vector");

        var indexes = await client.DescribeIndexAsync(collectionName, "float_vector");
        var index = Assert.Single(indexes);

        Assert.Equal("float_vector_idx", index.IndexName);
        Assert.Equal("float_vector", index.FieldName);
        var parameters = index.Params;

        Assert.Contains(parameters, kv => kv is { Key: "index_type", Value: "FLAT" });
        Assert.Contains(parameters, kv => kv is { Key: "metric_type", Value: "L2" });

        // TODO: Look into making this a nice structured dictionary rather than a serialized string
        Assert.Equal("""{"nlist":1024}""", parameters["params"]);
    }

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Drop(IMilvusClient client)
    {
        var collectionName = await CreateCollection(client);
        await client.CreateIndexAsync(
            collectionName, "float_vector", "float_vector_idx", MilvusIndexType.FLAT, MilvusMetricType.L2);
        await WaitForIndexBuild(client, collectionName, "float_vector");

        await client.DropIndexAsync(collectionName, "float_vector", "float_vector_idx");

        Assert.Equal(IndexState.None, await client.GetIndexStateAsync(collectionName, "float_vector"));
    }

    private async Task WaitForIndexBuild(IMilvusClient client, string collectionName, string fieldName)
    {
        while (true)
        {
            var indexState = await client.GetIndexStateAsync(collectionName, fieldName);
            if (indexState == IndexState.Finished)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }

    private async Task<string> CreateCollection(IMilvusClient client)
    {
        await client.DropCollectionAsync(nameof(IndexTests));
        await client.CreateCollectionAsync(
            nameof(IndexTests),
            new[]
            {
                FieldType.Create<long>("id", isPrimaryKey: true),
                FieldType.CreateVarchar("varchar", 256),
                FieldType.CreateFloatVector("float_vector", 1)
            });

        return nameof(IndexTests);
    }
}
