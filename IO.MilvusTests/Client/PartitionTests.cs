using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public class PartitionTests
{
    [Fact]
    public async Task Create()
    {
        string collectionName = await CreateCollection();

        await Client.CreatePartitionAsync(collectionName, "partition");
    }

    [Fact]
    public async Task Exists()
    {
        string collectionName = await CreateCollection();

        await Client.CreatePartitionAsync(collectionName, "partition");
        Assert.True(await Client.HasPartitionAsync(collectionName, "partition"));
    }

    [Fact]
    public async Task List()
    {
        string collectionName = await CreateCollection();

        await Client.CreatePartitionAsync(collectionName, "partition1");
        await Client.CreatePartitionAsync(collectionName, "partition2");

        var partitions = await Client.ShowPartitionsAsync(collectionName);

        Assert.Contains(partitions, p => p.PartitionName == "partition1");
        Assert.Contains(partitions, p => p.PartitionName == "partition2");
    }

    [Fact]
    public async Task Load_and_Release()
    {
        string collectionName = await CreateCollection();

        await Client.CreatePartitionAsync(collectionName, "partition");
        await Client.CreateIndexAsync(
            collectionName, "float_vector", MilvusIndexType.Flat,
            MilvusSimilarityMetricType.L2, new Dictionary<string, string>(), "float_vector_idx");
        await WaitForIndexBuild(collectionName, "float_vector");

        await Client.LoadPartitionsAsync(collectionName, new[] { "partition" });
        await Client.ReleasePartitionAsync(collectionName, new[] { "partition" });
    }

    [Fact]
    public async Task Drop()
    {
        string collectionName = await CreateCollection();

        await Client.DropPartitionsAsync(collectionName, "partition");
        Assert.False(await Client.HasPartitionAsync(collectionName, "partition"));
    }

    private async Task<string> CreateCollection()
    {
        await Client.DropCollectionAsync(nameof(PartitionTests));
        await Client.CreateCollectionAsync(
            nameof(PartitionTests),
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 1)
            });

        return nameof(PartitionTests);
    }

    private async Task WaitForIndexBuild(string collectionName, string fieldName)
    {
        while (true)
        {
            var indexState = await Client.GetIndexStateAsync(collectionName, fieldName);
            if (indexState == IndexState.Finished)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }

    private MilvusClient Client => TestEnvironment.Client;
}
