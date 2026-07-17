using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class PartitionTests : IAsyncLifetime
{
    [Fact]
    public async Task Create()
    {
        await Collection.CreatePartitionAsync("partition", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Exists()
    {
        await Collection.CreatePartitionAsync("partition", TestContext.Current.CancellationToken);
        Assert.True(await Collection.HasPartitionAsync("partition", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task List()
    {
        await Collection.CreatePartitionAsync("partition1", TestContext.Current.CancellationToken);
        await Collection.CreatePartitionAsync("partition2", TestContext.Current.CancellationToken);

        var partitions = await Collection.ShowPartitionsAsync(TestContext.Current.CancellationToken);

        Assert.Contains(partitions, p => p.PartitionName == "partition1");
        Assert.Contains(partitions, p => p.PartitionName == "partition2");
    }

    [Fact]
    public async Task Load_and_Release()
    {
        await Collection.CreatePartitionAsync("partition", TestContext.Current.CancellationToken);
        await Collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2, "float_vector_idx", new Dictionary<string, string>(), TestContext.Current.CancellationToken);
        await Collection.WaitForIndexBuildAsync("float_vector", cancellationToken: TestContext.Current.CancellationToken);

        await Collection.LoadPartitionsAsync(new[] { "partition" }, cancellationToken: TestContext.Current.CancellationToken);
        await Collection.ReleasePartitionsAsync(new[] { "partition" }, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Drop()
    {
        await Collection.DropPartitionAsync("partition", TestContext.Current.CancellationToken);
        Assert.False(await Collection.HasPartitionAsync("partition", TestContext.Current.CancellationToken));
    }

    public PartitionTests(MilvusFixture milvusFixture)
    {
        Client = milvusFixture.CreateClient();
        Collection = Client.GetCollection(CollectionName);
    }

    public async ValueTask InitializeAsync()
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

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }

    private const string CollectionName = nameof(PartitionTests);
    private readonly MilvusClient Client;

    private MilvusCollection Collection { get; }
}
