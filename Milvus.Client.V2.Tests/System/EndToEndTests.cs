using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Responses.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests;

[Trait("Category", "System")]
public class EndToEndTests : IAsyncLifetime
{
    private readonly MilvusV2Fixture _fixture;
    private MilvusClientV2 _client = null!;

    public EndToEndTests(MilvusV2Fixture fixture) => _fixture = fixture;

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
    public async Task Insert_Search_Query_full_flow()
    {
        const string collectionName = nameof(Insert_Search_Query_full_flow);

        await _client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName },
            TestContext.Current.CancellationToken);

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

        await _client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            FieldName = "embedding",
            IndexType = IndexType.Flat,
            MetricType = SimilarityMetricType.L2
        }, TestContext.Current.CancellationToken);

        MutationResp mutation = await _client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            Data =
            [
                FieldData.Create("id", new long[] { 1, 2, 3 }),
                FieldData.CreateVarChar("title", new[] { "first", "second", "third" }),
                FieldData.CreateFloatVector("embedding", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f, 0.3f, 0.4f }),
                    new ReadOnlyMemory<float>(new[] { 0.5f, 0.1f, 0.2f, 0.3f }),
                    new ReadOnlyMemory<float>(new[] { 0.9f, 0.8f, 0.7f, 0.6f })
                })
            ]
        }, TestContext.Current.CancellationToken);

        Assert.Equal(3, mutation.InsertCount);

        await _client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName },
            TestContext.Current.CancellationToken);

        await WaitForLoadAsync(collectionName);

        SearchResp search = await _client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "embedding",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f, 0.3f, 0.4f }) },
            MetricType = SimilarityMetricType.L2,
            Limit = 2,
            Parameters = new SearchParameters { OutputFields = { "title" } }
        }, TestContext.Current.CancellationToken);

        Assert.NotNull(search.Ids.LongIds);
        Assert.Equal(1L, search.Ids.LongIds![0]);
        Assert.NotEmpty(search.Scores);

        QueryResp query = await _client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id in [1, 2]",
            Parameters = new QueryParameters { OutputFields = { "id", "title" } }
        }, TestContext.Current.CancellationToken);

        var idField = (FieldData<long>)query.FieldsData.Single(f => f.FieldName == "id");
        Assert.Equal(2, idField.Data.Count);

        await _client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName },
            TestContext.Current.CancellationToken);
    }

    private async Task WaitForLoadAsync(string collectionName)
    {
        for (int i = 0; i < 30; i++)
        {
            GetLoadStateResp state = await _client.GetLoadStateAsync(new GetLoadStateReq { CollectionName = collectionName },
                TestContext.Current.CancellationToken);
            if (state.State == LoadState.Loaded)
            {
                return;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
