using Xunit;

using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Responses.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests;

[Trait("Category", "System")]
[Collection(MilvusV2Collection.Name)]
public class IndexSystemTests : SystemTestBase
{
    public IndexSystemTests(MilvusV2Fixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Create_Describe_Drop_Index_round_trip()
    {
        const string collectionName = nameof(Create_Describe_Drop_Index_round_trip);

        await ResetCollectionAsync(collectionName);

        await Client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("embedding", dimension: 4)
                }
            }
        }, TestContext.Current.CancellationToken);

        await Client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [ new IndexParam("embedding", "idx_embedding", IndexType.Flat, SimilarityMetricType.L2) ]
        }, TestContext.Current.CancellationToken);

        DescribeIndexResp describe = await Client.DescribeIndexAsync(new DescribeIndexReq
        {
            CollectionName = collectionName,
            FieldName = "embedding",
            IndexName = "idx_embedding"
        }, TestContext.Current.CancellationToken);

        Assert.Single(describe.Indexes);
        IndexDesc index = describe.Indexes[0];
        Assert.Equal("idx_embedding", index.IndexName);
        Assert.Equal("embedding", index.FieldName);

        await Client.DropIndexAsync(new DropIndexReq
        {
            CollectionName = collectionName,
            FieldName = "embedding",
            IndexName = "idx_embedding"
        }, TestContext.Current.CancellationToken);

        await ResetCollectionAsync(collectionName);
    }
}
