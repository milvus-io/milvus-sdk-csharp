using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests;

/// <summary>
/// Shared base for real-container (<c>System</c>) test classes: holds the fixture, creates a per-test client,
/// and exposes common helpers like waiting for a collection to load and resetting collections. Each System
/// test class is grouped by function area and inherits from this base.
/// </summary>
[Trait("Category", "System")]
[Collection(MilvusV2Collection.Name)]
public abstract class SystemTestBase : IAsyncLifetime
{
    private readonly MilvusV2Fixture _fixture;

    protected SystemTestBase(MilvusV2Fixture fixture) => _fixture = fixture;

    /// <summary>
    /// A client bound to the shared real Milvus container, created fresh for each test.
    /// </summary>
    protected MilvusClientV2 Client { get; private set; } = null!;

    public ValueTask InitializeAsync()
    {
        Client = _fixture.CreateClient();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Drops the collection if it exists, so each test starts clean.
    /// </summary>
    protected async Task ResetCollectionAsync(string collectionName)
    {
        HasCollectionResp has = await Client.HasCollectionAsync(
            new HasCollectionReq { CollectionName = collectionName }, TestContext.Current.CancellationToken);
        if (has.Has)
        {
            await Client.DropCollectionAsync(
                new DropCollectionReq { CollectionName = collectionName }, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Polls GetLoadState until the collection is loaded.
    /// </summary>
    protected async Task WaitForLoadAsync(string collectionName)
    {
        for (int i = 0; i < 30; i++)
        {
            GetLoadStateResp state = await Client.GetLoadStateAsync(
                new GetLoadStateReq { CollectionName = collectionName }, TestContext.Current.CancellationToken);
            if (state.State == LoadState.Loaded)
            {
                return;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
