namespace Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Creates a resource group, a named pool of query nodes that collection replicas can be loaded
    /// into. Available since Milvus v2.4.
    /// </summary>
    /// <param name="resourceGroupName">The name of the resource group to create.</param>
    /// <param name="config">
    /// An optional configuration describing how many query nodes the group should hold. When omitted,
    /// the group is created empty and nodes must be moved in explicitly.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <remarks>
    /// Milvus always has a default resource group holding every query node that is not assigned
    /// elsewhere; nodes placed in a new group are taken from it.
    /// </remarks>
    public async Task CreateResourceGroupAsync(
        string resourceGroupName,
        ResourceGroupConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(resourceGroupName);

        CreateResourceGroupRequest request = new() { ResourceGroup = resourceGroupName };

        if (config is not null)
        {
            request.Config = config.ToGrpc();
        }

        await InvokeAsync(GrpcClient.CreateResourceGroupAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Lists the names of all resource groups. Available since Milvus v2.4.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>The names of all resource groups, including the default one.</returns>
    public async Task<IReadOnlyList<string>> ListResourceGroupsAsync(
        CancellationToken cancellationToken = default)
    {
        ListResourceGroupsResponse response = await InvokeAsync(
                GrpcClient.ListResourceGroupsAsync, new ListResourceGroupsRequest(), static r => r.Status,
                cancellationToken)
            .ConfigureAwait(false);

        return response.ResourceGroups;
    }

    /// <summary>
    /// Returns the observed state of a resource group: its configuration, the query nodes it holds, and
    /// the replicas loaded into it. Available since Milvus v2.4.
    /// </summary>
    /// <param name="resourceGroupName">The name of the resource group to describe.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<ResourceGroupDescription> DescribeResourceGroupAsync(
        string resourceGroupName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(resourceGroupName);

        DescribeResourceGroupResponse response = await InvokeAsync(
                GrpcClient.DescribeResourceGroupAsync,
                new DescribeResourceGroupRequest { ResourceGroup = resourceGroupName },
                static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return ResourceGroupDescription.FromGrpc(response.ResourceGroup);
    }

    /// <summary>
    /// Updates the configuration of one or more resource groups. Available since Milvus v2.4.
    /// </summary>
    /// <param name="configs">The new configuration, keyed by resource group name.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <remarks>
    /// Updating several groups in one call lets Milvus apply the whole change as a single rebalance,
    /// which is why this takes a map rather than a single group. This is the supported way to move
    /// query nodes between groups.
    /// </remarks>
    public async Task UpdateResourceGroupsAsync(
        IReadOnlyDictionary<string, ResourceGroupConfig> configs,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(configs);

        if (configs.Count == 0)
        {
            throw new ArgumentException("At least one resource group configuration must be provided.",
                nameof(configs));
        }

        UpdateResourceGroupsRequest request = new();

        foreach (KeyValuePair<string, ResourceGroupConfig> config in configs)
        {
            Verify.NotNullOrWhiteSpace(config.Key);
            Verify.NotNull(config.Value);

            request.ResourceGroups.Add(config.Key, config.Value.ToGrpc());
        }

        await InvokeAsync(GrpcClient.UpdateResourceGroupsAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a resource group. Available since Milvus v2.4.
    /// </summary>
    /// <param name="resourceGroupName">The name of the resource group to drop.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <remarks>
    /// <para>
    /// The group must be empty. In particular its configured limits must be back to zero, otherwise
    /// Milvus rejects the call with <c>"resource group's limits node num is not 0"</c> — call
    /// <see cref="UpdateResourceGroupsAsync" /> with a zeroed
    /// <see cref="ResourceGroupConfig" /> first. Any loaded replicas must also have been moved away
    /// with <see cref="TransferReplicaAsync" />.
    /// </para>
    /// </remarks>
    public async Task DropResourceGroupAsync(
        string resourceGroupName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(resourceGroupName);

        await InvokeAsync(
                GrpcClient.DropResourceGroupAsync,
                new DropResourceGroupRequest { ResourceGroup = resourceGroupName }, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Moves loaded replicas of a collection from one resource group to another. Available since
    /// Milvus v2.4.
    /// </summary>
    /// <param name="sourceResourceGroup">The resource group currently holding the replicas.</param>
    /// <param name="targetResourceGroup">The resource group to move them to.</param>
    /// <param name="collectionName">The collection whose replicas should be moved.</param>
    /// <param name="replicaCount">The number of replicas to move.</param>
    /// <param name="databaseName">
    /// An optional database name. Defaults to the database this client is connected to.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task TransferReplicaAsync(
        string sourceResourceGroup,
        string targetResourceGroup,
        string collectionName,
        long replicaCount,
        string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(sourceResourceGroup);
        Verify.NotNullOrWhiteSpace(targetResourceGroup);
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.GreaterThan(replicaCount, 0);

        TransferReplicaRequest request = new()
        {
            SourceResourceGroup = sourceResourceGroup,
            TargetResourceGroup = targetResourceGroup,
            CollectionName = collectionName,
            NumReplica = replicaCount
        };

        if (databaseName is not null)
        {
            request.DbName = databaseName;
        }

        await InvokeAsync(GrpcClient.TransferReplicaAsync, request, cancellationToken).ConfigureAwait(false);
    }
}
