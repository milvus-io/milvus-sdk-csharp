using Xunit;

namespace Milvus.Client.Tests;

/// <summary>
/// Resource groups redistribute query nodes across the whole cluster, so these tests are kept out of
/// the parallel pool: a rebalance triggered here can otherwise disturb collections other test classes
/// are loading.
/// </summary>
[CollectionDefinition(nameof(ResourceGroupTests), DisableParallelization = true)]
public sealed class ResourceGroupTestsCollection;

[Collection(nameof(ResourceGroupTests))]
public class ResourceGroupTests(MilvusFixture milvusFixture) : IAsyncLifetime
{
    [Fact]
    public async Task Create_List_Describe_Drop()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        Assert.DoesNotContain(GroupName,
            await Client.ListResourceGroupsAsync(TestContext.Current.CancellationToken));

        // Request zero nodes: the Testcontainer runs a single query node, and claiming it would leave
        // the default resource group unable to serve the other test classes.
        await Client.CreateResourceGroupAsync(
            GroupName, new ResourceGroupConfig(requestsNodeNum: 0, limitsNodeNum: 0),
            TestContext.Current.CancellationToken);

        Assert.Contains(GroupName, await Client.ListResourceGroupsAsync(TestContext.Current.CancellationToken));

        ResourceGroupDescription description =
            await Client.DescribeResourceGroupAsync(GroupName, TestContext.Current.CancellationToken);

        Assert.Equal(GroupName, description.Name);
        Assert.Equal(0, description.Config.RequestsNodeNum);
        Assert.Equal(0, description.Config.LimitsNodeNum);
        Assert.Empty(description.Nodes);
        Assert.NotNull(description.LoadedReplicaCounts);

        // Dropping works directly here because the group was created with limits already at 0.
        await Client.DropResourceGroupAsync(GroupName, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(GroupName,
            await Client.ListResourceGroupsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Update_config()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        await Client.CreateResourceGroupAsync(
            GroupName, new ResourceGroupConfig(requestsNodeNum: 0, limitsNodeNum: 0),
            TestContext.Current.CancellationToken);

        await Client.UpdateResourceGroupsAsync(
            new Dictionary<string, ResourceGroupConfig>
            {
                [GroupName] = new(
                    requestsNodeNum: 0,
                    limitsNodeNum: 1,
                    transferFrom: new[] { "__default_resource_group" },
                    transferTo: new[] { "__default_resource_group" })
            }, TestContext.Current.CancellationToken);

        ResourceGroupDescription description =
            await Client.DescribeResourceGroupAsync(GroupName, TestContext.Current.CancellationToken);

        Assert.Equal(0, description.Config.RequestsNodeNum);
        Assert.Equal(1, description.Config.LimitsNodeNum);
        Assert.Contains("__default_resource_group", description.Config.TransferFrom);
        Assert.Contains("__default_resource_group", description.Config.TransferTo);

        await DropGroupAsync(GroupName);
    }

    [Fact]
    public async Task Default_resource_group_is_always_present()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        IReadOnlyList<string> groups = await Client.ListResourceGroupsAsync(TestContext.Current.CancellationToken);

        // Every query node not explicitly assigned elsewhere lives in the default group, so it exists
        // even on a cluster where no resource group was ever created.
        Assert.Contains("__default_resource_group", groups);

        ResourceGroupDescription description = await Client.DescribeResourceGroupAsync(
            "__default_resource_group", TestContext.Current.CancellationToken);

        Assert.Equal("__default_resource_group", description.Name);
        Assert.NotEmpty(description.Nodes);
        Assert.All(description.Nodes, node => Assert.True(node.NodeId > 0));
    }

    [Fact]
    public async Task UpdateResourceGroups_rejects_empty_map()
        => await Assert.ThrowsAsync<ArgumentException>(() =>
            Client.UpdateResourceGroupsAsync(
                new Dictionary<string, ResourceGroupConfig>(), TestContext.Current.CancellationToken));

    [Fact]
    public void Config_rejects_limits_below_requests()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => new ResourceGroupConfig(requestsNodeNum: 2, limitsNodeNum: 1));

    public async ValueTask InitializeAsync()
    {
        // Clean up after a previous failed run.
        if (await Client.GetParsedMilvusVersion() >= new Version(2, 4)
            && (await Client.ListResourceGroupsAsync()).Contains(GroupName))
        {
            await DropGroupAsync(GroupName);
        }
    }

    /// <summary>
    /// Milvus refuses to drop a resource group whose limits node num is not 0 ("expected=not empty
    /// resource group"), so shrink the group to zero capacity first.
    /// </summary>
    private async Task DropGroupAsync(string name)
    {
        await Client.UpdateResourceGroupsAsync(
            new Dictionary<string, ResourceGroupConfig>
            {
                [name] = new(requestsNodeNum: 0, limitsNodeNum: 0)
            });

        await Client.DropResourceGroupAsync(name);
    }

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }

    private readonly MilvusClient Client = milvusFixture.CreateClient();

    private const string GroupName = nameof(ResourceGroupTests);
}
