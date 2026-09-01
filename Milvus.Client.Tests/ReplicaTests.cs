using Xunit;

namespace Milvus.Client.Tests;

public class ReplicaTests(MilvusFixture milvusFixture) : IAsyncLifetime
{
    [Fact]
    public async Task GetReplicas_describes_a_loaded_collection()
    {
        MilvusCollection collection = await CreateLoadedCollectionAsync(
            nameof(GetReplicas_describes_a_loaded_collection));

        IReadOnlyList<MilvusReplicaInfo> replicas =
            await Client.GetReplicasAsync(collection.Name, cancellationToken: TestContext.Current.CancellationToken);

        MilvusReplicaInfo replica = Assert.Single(replicas);

        Assert.True(replica.ReplicaId > 0);
        Assert.True(replica.CollectionId > 0);

        // Loading the whole collection rather than named partitions leaves the partition list empty.
        Assert.Empty(replica.PartitionIds);

        Assert.NotEmpty(replica.NodeIds);
        Assert.All(replica.NodeIds, nodeId => Assert.True(nodeId > 0));

        // One DML channel means one shard, and the container runs a single query node, so that node is
        // necessarily the shard leader.
        MilvusShardReplica shard = Assert.Single(replica.ShardReplicas);
        Assert.Contains(shard.LeaderId, replica.NodeIds);
        Assert.Contains(":", shard.LeaderAddress);
        Assert.NotEmpty(shard.DmChannelName);

        if (await Client.GetParsedMilvusVersion() >= new Version(2, 4))
        {
            Assert.Equal("__default_resource_group", replica.ResourceGroupName);
        }

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetReplicas_handles_an_unloaded_collection()
    {
        MilvusCollection collection =
            await CreateCollectionAsync(nameof(GetReplicas_handles_an_unloaded_collection));

        if (await Client.GetParsedMilvusVersion() >= new Version(2, 4))
        {
            // A replica is a property of being loaded, not of existing, so this is an empty result
            // rather than an error.
            Assert.Empty(await Client.GetReplicasAsync(
                collection.Name, cancellationToken: TestContext.Current.CancellationToken));
        }
        else
        {
            // 2.3 looks the replica up by id and reports it missing instead of returning nothing.
            MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
                Client.GetReplicasAsync(
                    collection.Name, cancellationToken: TestContext.Current.CancellationToken));

            Assert.Contains("replica not found", exception.Message);
        }

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetReplicas_throws_for_a_collection_that_does_not_exist()
    {
        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            Client.GetReplicasAsync(
                "replica_tests_no_such_collection",
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("replica_tests_no_such_collection", exception.Message);
    }

    /// <summary>
    /// Canary for the inverted <c>with_shard_nodes</c> flag that
    /// <see cref="MilvusClient.GetReplicasAsync" /> documents and picks its default from. If Milvus
    /// ever makes the flag behave the way the proto describes, this fails and the default should be
    /// flipped back to <see langword="true" />.
    /// </summary>
    [Fact]
    public async Task GetReplicas_shard_node_flag_is_inverted_server_side()
    {
        MilvusCollection collection = await CreateLoadedCollectionAsync(
            nameof(GetReplicas_shard_node_flag_is_inverted_server_side));

        IReadOnlyList<MilvusReplicaInfo> withFlagUnset = await Client.GetReplicasAsync(
            collection.Name, withShardNodes: false, cancellationToken: TestContext.Current.CancellationToken);

        IReadOnlyList<MilvusReplicaInfo> withFlagSet = await Client.GetReplicasAsync(
            collection.Name, withShardNodes: true, cancellationToken: TestContext.Current.CancellationToken);

        // Backwards on purpose: false is what actually populates the per-shard node list.
        Assert.NotEmpty(Assert.Single(Assert.Single(withFlagUnset).ShardReplicas).NodeIds);
        Assert.Empty(Assert.Single(Assert.Single(withFlagSet).ShardReplicas).NodeIds);

        // The replica-level node list is unaffected either way.
        Assert.NotEmpty(Assert.Single(withFlagUnset).NodeIds);
        Assert.NotEmpty(Assert.Single(withFlagSet).NodeIds);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetReplicas_rejects_empty_collection_name()
        => await Assert.ThrowsAsync<ArgumentException>(() =>
            Client.GetReplicasAsync(" ", cancellationToken: TestContext.Current.CancellationToken));

    private async Task<MilvusCollection> CreateCollectionAsync(string name)
    {
        MilvusCollection collection = Client.GetCollection(name);
        await collection.DropAsync(TestContext.Current.CancellationToken);

        await Client.CreateCollectionAsync(
            name,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("vector", 2)
            }, cancellationToken: TestContext.Current.CancellationToken);

        await collection.CreateIndexAsync(
            "vector", IndexType.Flat, SimilarityMetricType.L2, "vector_idx",
            new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        return collection;
    }

    private async Task<MilvusCollection> CreateLoadedCollectionAsync(string name)
    {
        MilvusCollection collection = await CreateCollectionAsync(name);

        await collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new[] { 1L, 2L }),
                FieldData.CreateFloatVector(
                    "vector",
                    new[]
                    {
                        new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                        new ReadOnlyMemory<float>(new[] { 0f, 1f })
                    })
            }, cancellationToken: TestContext.Current.CancellationToken);

        await collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1),
            cancellationToken: TestContext.Current.CancellationToken);

        return collection;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }

    private readonly MilvusClient Client = milvusFixture.CreateClient();
}
