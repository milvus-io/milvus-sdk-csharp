using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public class PartitionTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Create(IMilvusClient client)
    {
        var collectionName = await CreateCollection(client);

        await client.CreatePartitionAsync(collectionName, "partition");
    }

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Exists(IMilvusClient client)
    {
        var collectionName = await CreateCollection(client);

        await client.CreatePartitionAsync(collectionName, "partition");
        Assert.True(await client.HasPartitionAsync(collectionName, "partition"));
    }

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task List(IMilvusClient client)
    {
        var collectionName = await CreateCollection(client);

        await client.CreatePartitionAsync(collectionName, "partition1");
        await client.CreatePartitionAsync(collectionName, "partition2");

        var partitions = await client.ShowPartitionsAsync(collectionName);

        Assert.Contains(partitions, p => p.PartitionName == "partition1");
        Assert.Contains(partitions, p => p.PartitionName == "partition2");
    }

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Load_and_Release(IMilvusClient client)
    {
        var collectionName = await CreateCollection(client);

        await client.CreatePartitionAsync(collectionName, "partition");
        await client.CreateIndexAsync(
            collectionName, "float_vector", "float_vector_idx", MilvusIndexType.FLAT, MilvusMetricType.L2);
        await WaitForIndexBuild(client, collectionName, "float_vector");

        await client.LoadPartitionsAsync(collectionName, new[] { "partition" });
        await client.ReleasePartitionAsync(collectionName, new[] { "partition" });
    }

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Drop(IMilvusClient client)
    {
        var collectionName = await CreateCollection(client);

        await client.DropPartitionsAsync(collectionName, "partition");
        Assert.False(await client.HasPartitionAsync(collectionName, "partition"));
    }

    private async Task<string> CreateCollection(IMilvusClient client)
    {
        await client.DropCollectionAsync(nameof(PartitionTests));
        await client.CreateCollectionAsync(
            nameof(PartitionTests),
            new[]
            {
                FieldType.Create<long>("id", isPrimaryKey: true),
                FieldType.CreateFloatVector("float_vector", 1)
            });

        return nameof(PartitionTests);
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
}
