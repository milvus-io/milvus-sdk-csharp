using Xunit;

namespace Milvus.Client.Tests;

public class PartitionTests : IAsyncLifetime
{
    [Fact]
    public async Task Create()
    {
        await Collection.CreatePartitionAsync("partition");
    }

    [Fact]
    public async Task Exists()
    {
        await Collection.CreatePartitionAsync("partition");
        Assert.True(await Collection.HasPartitionAsync("partition"));
    }

    [Fact]
    public async Task List()
    {
        await Collection.CreatePartitionAsync("partition1");
        await Collection.CreatePartitionAsync("partition2");

        var partitions = await Collection.ShowPartitionsAsync();

        Assert.Contains(partitions, p => p.PartitionName == "partition1");
        Assert.Contains(partitions, p => p.PartitionName == "partition2");
    }

    [Fact]
    public async Task Load_and_Release()
    {
        await Collection.CreatePartitionAsync("partition");
        await Collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2,
            new Dictionary<string, string>(), "float_vector_idx");
        await Collection.WaitForIndexBuildAsync("float_vector");

        await Collection.LoadPartitionsAsync(new[] { "partition" });
        await Collection.ReleasePartitionsAsync(new[] { "partition" });
    }

    [Fact]
    public async Task Drop()
    {
        await Collection.DropPartitionAsync("partition");
        Assert.False(await Collection.HasPartitionAsync("partition"));
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

    private const string CollectionName = nameof(PartitionTests);
    private MilvusClient Client => TestEnvironment.Client;

    private MilvusCollection Collection { get; }

    public PartitionTests()
        => Collection = Client.GetCollection(CollectionName);
}
