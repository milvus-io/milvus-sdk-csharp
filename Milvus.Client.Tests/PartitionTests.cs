using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
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
            "float_vector", IndexType.Flat, SimilarityMetricType.L2, "float_vector_idx", new Dictionary<string, string>());
        await Collection.WaitForIndexBuildAsync("float_vector");

        await Collection.LoadPartitionsAsync(["partition"]);
        await Collection.ReleasePartitionsAsync(["partition"]);
    }

    [Fact]
    public async Task Drop()
    {
        await Collection.DropPartitionAsync("partition");
        Assert.False(await Collection.HasPartitionAsync("partition"));
    }

    public PartitionTests(MilvusFixture milvusFixture)
    {
        Client = milvusFixture.CreateClient();
        Collection = Client.GetCollection(CollectionName);
    }

    public async Task InitializeAsync()
    {
        await Collection.DropAsync();
        await Client.CreateCollectionAsync(
            CollectionName,
            [
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateFloatVector("float_vector", 4),
            ]);
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    private const string CollectionName = nameof(PartitionTests);
    private readonly MilvusClient Client;

    private MilvusCollection Collection { get; }
}
