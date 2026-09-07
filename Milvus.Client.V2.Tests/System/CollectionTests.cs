using Xunit;

using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests;

[Trait("Category", "System")]
public class CollectionTests : IAsyncLifetime
{
    private readonly MilvusV2Fixture _fixture;
    private MilvusClientV2 _client = null!;

    public CollectionTests(MilvusV2Fixture fixture) => _fixture = fixture;

    public ValueTask InitializeAsync()
    {
        _client = _fixture.CreateClient();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Create_Has_List_Drop()
    {
        const string collectionName = nameof(Create_Has_List_Drop);

        // Start clean.
        await _client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName }, TestContext.Current.CancellationToken);

        // It shouldn't exist yet.
        HasCollectionResp has = await _client.HasCollectionAsync(new HasCollectionReq { CollectionName = collectionName }, TestContext.Current.CancellationToken);
        Assert.False(has.Has);

        // Create it via the DTO pattern.
        await _client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar("title", maxLength: 200),
                    FieldSchema.CreateFloatVector("embedding", dimension: 4)
                }
            }
        }, TestContext.Current.CancellationToken);

        // It should exist now, and show up in the list.
        has = await _client.HasCollectionAsync(new HasCollectionReq { CollectionName = collectionName }, TestContext.Current.CancellationToken);
        Assert.True(has.Has);

        ListCollectionsResp list = await _client.ListCollectionsAsync(new ListCollectionsReq(), TestContext.Current.CancellationToken);
        Assert.Contains(collectionName, list.CollectionNames);

        // Drop it and verify.
        await _client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName }, TestContext.Current.CancellationToken);

        has = await _client.HasCollectionAsync(new HasCollectionReq { CollectionName = collectionName }, TestContext.Current.CancellationToken);
        Assert.False(has.Has);
    }

    [Fact]
    public async Task Create_collection_throws_for_missing_schema()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _client.CreateCollectionAsync(new CreateCollectionReq { CollectionName = "no_schema" }, TestContext.Current.CancellationToken));
    }
}
