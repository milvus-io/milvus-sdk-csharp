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

    /// <summary>
    /// Returns the in-memory replicas of a loaded collection, and which query nodes serve them.
    /// </summary>
    /// <param name="collectionName">The collection whose replicas should be returned.</param>
    /// <param name="withShardNodes">
    /// The proto's <c>with_shard_nodes</c> flag, passed through unchanged. Controls whether
    /// <see cref="MilvusShardReplica.NodeIds" /> is populated — see the remarks, because the server
    /// treats it the opposite way round from how it reads.
    /// </param>
    /// <param name="databaseName">
    /// An optional database name. Defaults to the database this client is connected to.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>
    /// One entry per replica. A collection loaded with one replica returns a single entry. From Milvus
    /// 2.4 an unloaded collection returns an empty list; see the remarks for 2.3.
    /// </returns>
    /// <exception cref="MilvusException">
    /// The collection does not exist, or — on Milvus 2.3 only — is not loaded.
    /// </exception>
    /// <remarks>
    /// <para>
    /// A replica only exists while the collection is loaded, and the two release lines disagree about
    /// what that means for an unloaded one. Milvus 2.4 and later treat it as simply having no replicas
    /// and return an empty list; 2.3 looks the replica up by id and fails with <c>"replica not
    /// found"</c>. Verified against 2.3.22, 2.4.23, 2.5.20 and 2.6.4. A collection that does not exist
    /// at all throws on every version.
    /// </para>
    /// <para>
    /// <paramref name="withShardNodes" /> defaults to <see langword="false" /> because Milvus has the
    /// flag backwards: the proto says <c>node_ids</c> is "set only for GetReplicas() if
    /// with_shard_nodes is true", but sending <see langword="true" /> returns per-shard node lists
    /// that are <em>empty</em>, and sending <see langword="false" /> returns them populated. Verified
    /// against Milvus 2.3.22, 2.4.23 and 2.6.4, so this is longstanding rather than a regression. The
    /// value is passed through rather than negated, so a server that fixes this keeps working — only
    /// this note goes stale. Note that the replica-level <see cref="MilvusReplicaInfo.NodeIds" /> is
    /// populated either way; the flag only affects the per-shard lists.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<MilvusReplicaInfo>> GetReplicasAsync(
        string collectionName,
        bool withShardNodes = false,
        string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        GetReplicasRequest request = new()
        {
            CollectionName = collectionName,
            WithShardNodes = withShardNodes
        };

        if (databaseName is not null)
        {
            request.DbName = databaseName;
        }

        GetReplicasResponse response = await InvokeAsync(
                GrpcClient.GetReplicasAsync, request, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        List<MilvusReplicaInfo> replicas = new(response.Replicas.Count);

        foreach (Grpc.ReplicaInfo replica in response.Replicas)
        {
            replicas.Add(MilvusReplicaInfo.FromGrpc(replica));
        }

        return replicas;
    }

    /// <summary>
    /// Manually moves sealed segments off one query node onto others, to even out load.
    /// </summary>
    /// <param name="collectionName">The collection whose segments should be moved.</param>
    /// <param name="sourceNodeId">
    /// The query node to move segments off. Get candidate ids from
    /// <see cref="MilvusReplicaInfo.NodeIds" /> or <see cref="ResourceGroupDescription.Nodes" />.
    /// </param>
    /// <param name="targetNodeIds">
    /// The query nodes to move segments onto. When null or empty, Milvus picks the targets itself.
    /// </param>
    /// <param name="sealedSegmentIds">
    /// The specific sealed segments to move. When null or empty, Milvus moves whatever it considers
    /// necessary to balance the source node.
    /// </param>
    /// <param name="databaseName">
    /// An optional database name. Defaults to the database this client is connected to.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <exception cref="MilvusException">
    /// The collection is not loaded, or <paramref name="sourceNodeId" /> is not serving any replica of
    /// it.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Only sealed segments can be moved; growing segments stay on the node that is writing them until
    /// they seal. The collection must be loaded, and the source node must actually be serving it —
    /// both are server-side errors rather than empty successes.
    /// </para>
    /// <para>
    /// This is a manual override of the balancing Milvus already does on its own, so it is mostly
    /// useful for draining a specific node. On a single-query-node deployment there is nowhere to move
    /// segments to, and Milvus accepts the request and does nothing rather than reporting an error, so
    /// a successful return is not by itself evidence that anything moved.
    /// </para>
    /// </remarks>
    public async Task LoadBalanceAsync(
        string collectionName,
        long sourceNodeId,
        IReadOnlyList<long>? targetNodeIds = null,
        IReadOnlyList<long>? sealedSegmentIds = null,
        string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        LoadBalanceRequest request = new()
        {
            CollectionName = collectionName,
            SrcNodeID = sourceNodeId
        };

        if (targetNodeIds is not null)
        {
            request.DstNodeIDs.AddRange(targetNodeIds);
        }

        if (sealedSegmentIds is not null)
        {
            request.SealedSegmentIDs.AddRange(sealedSegmentIds);
        }

        if (databaseName is not null)
        {
            request.DbName = databaseName;
        }

        await InvokeAsync(GrpcClient.LoadBalanceAsync, request, cancellationToken).ConfigureAwait(false);
    }
}
