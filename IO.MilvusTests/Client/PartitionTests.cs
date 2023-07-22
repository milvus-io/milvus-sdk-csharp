using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public class PartitionTests : IAsyncLifetime
{
    [Fact]
    public async Task Create()
    {
        await Client.CreatePartitionAsync(CollectionName, "partition");
    }

    [Fact]
    public async Task Exists()
    {
        await Client.CreatePartitionAsync(CollectionName, "partition");
        Assert.True(await Client.HasPartitionAsync(CollectionName, "partition"));
    }

    [Fact]
    public async Task List()
    {
        await Client.CreatePartitionAsync(CollectionName, "partition1");
        await Client.CreatePartitionAsync(CollectionName, "partition2");

        var partitions = await Client.ShowPartitionsAsync(CollectionName);

        Assert.Contains(partitions, p => p.PartitionName == "partition1");
        Assert.Contains(partitions, p => p.PartitionName == "partition2");
    }

    [Fact]
    public async Task Load_and_Release()
    {
        await Client.CreatePartitionAsync(CollectionName, "partition");
        await Client.CreateIndexAsync(
            CollectionName, "float_vector", MilvusIndexType.Flat,
            MilvusSimilarityMetricType.L2, new Dictionary<string, string>(), "float_vector_idx");
        await Client.WaitForIndexBuildAsync(CollectionName, "float_vector");

        await Client.LoadPartitionsAsync(CollectionName, new[] { "partition" });
        await Client.ReleasePartitionAsync(CollectionName, new[] { "partition" });
    }

    [Fact]
    public async Task Drop()
    {
        await Client.DropPartitionsAsync(CollectionName, "partition");
        Assert.False(await Client.HasPartitionAsync(CollectionName, "partition"));
    }

    public async Task InitializeAsync()
    {
        await Client.DropCollectionAsync(CollectionName);
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

    private const string CollectionName = nameof(PartitionTests);
    private MilvusClient Client => TestEnvironment.Client;
}
