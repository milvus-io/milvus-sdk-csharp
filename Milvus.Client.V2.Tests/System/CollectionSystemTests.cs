using Xunit;

using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests;

[Trait("Category", "System")]
[Collection(MilvusV2Collection.Name)]
public class CollectionSystemTests : SystemTestBase
{
    public CollectionSystemTests(MilvusV2Fixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Create_Has_List_Drop()
    {
        const string collectionName = nameof(Create_Has_List_Drop);

        await ResetCollectionAsync(collectionName);

        await Client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar("title", maxLength: 200),
                    FieldSchema.CreateFloatVector("dummy_vec", dimension: 2)
                }
            }
        }, TestContext.Current.CancellationToken);

        HasCollectionResp has = await Client.HasCollectionAsync(new HasCollectionReq { CollectionName = collectionName },
            TestContext.Current.CancellationToken);
        Assert.True(has.Has);

        ListCollectionsResp list = await Client.ListCollectionsAsync(new ListCollectionsReq(), TestContext.Current.CancellationToken);
        Assert.Contains(collectionName, list.CollectionNames);

        await Client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName },
            TestContext.Current.CancellationToken);
        has = await Client.HasCollectionAsync(new HasCollectionReq { CollectionName = collectionName },
            TestContext.Current.CancellationToken);
        Assert.False(has.Has);
    }

    [Fact]
    public async Task Create_collection_throws_for_missing_schema()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Client.CreateCollectionAsync(new CreateCollectionReq { CollectionName = "no_schema" }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DescribeCollection_returns_schema_fields()
    {
        const string collectionName = nameof(DescribeCollection_returns_schema_fields);

        await ResetCollectionAsync(collectionName);

        await Client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar("title", maxLength: 128),
                    FieldSchema.CreateFloatVector("embedding", dimension: 8)
                }
            }
        }, TestContext.Current.CancellationToken);

        DescribeCollectionResp description = await Client.DescribeCollectionAsync(
            new DescribeCollectionReq { CollectionName = collectionName }, TestContext.Current.CancellationToken);

        Assert.Equal(collectionName, description.CollectionName);
        Assert.Equal(3, description.Schema.Fields.Count);
        Assert.Contains(description.Schema.Fields, f => f.Name == "embedding" && f.Dimension == 8);

        await ResetCollectionAsync(collectionName);
    }

    [Fact]
    public async Task Struct_field_round_trips_insert_and_query()
    {
        const string collectionName = nameof(Struct_field_round_trips_insert_and_query);

        await ResetCollectionAsync(collectionName);

        await Client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true, autoId: true),
                    FieldSchema.CreateVarchar("title", maxLength: 200)
                },
                StructFields =
                {
                    new StructFieldSchema("st")
                    {
                        MaxCapacity = 8,
                        Fields =
                        {
                            new FieldSchema("int32", DataType.Int32),
                            FieldSchema.CreateVarchar("tag", maxLength: 16),
                            new FieldSchema("vector", DataType.FloatVector) { Dimension = 2 }
                        }
                    }
                }
            }
        }, TestContext.Current.CancellationToken);

        await Client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [ new IndexParam("st[vector]", null, IndexType.Hnsw, SimilarityMetricType.MaxSimCosine) ]
        }, TestContext.Current.CancellationToken);

        await Client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            RowsData =
            [
                new Dictionary<string, object?>
                {
                    ["title"] = "doc1",
                    ["st"] = new object[]
                    {
                        new Dictionary<string, object?> { ["int32"] = 1, ["tag"] = "a", ["vector"] = new[] { 1f, 2f } },
                        new Dictionary<string, object?> { ["int32"] = 2, ["tag"] = "b", ["vector"] = new[] { 3f, 4f } }
                    }
                }
            ]
        }, TestContext.Current.CancellationToken);

        await Client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName },
            TestContext.Current.CancellationToken);
        await WaitForLoadAsync(collectionName);

        QueryResp query = await Client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "title == \"doc1\"",
            Parameters = new QueryParameters { OutputFields = { "title", "st" } }
        }, TestContext.Current.CancellationToken);

        Assert.NotNull(query.FieldsData);
        FieldData structField = query.FieldsData.First(f => f.FieldName == "st");
        Assert.IsType<StructFieldData>(structField);
        var structData = (StructFieldData)structField;
        Assert.Single(structData.Data);
        // Sub-row order inside a struct array is not a documented guarantee; assert by key.
        var subRows = structData.Data[0]!.ToDictionary(
            r => Convert.ToInt32(r["int32"], System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(2, subRows.Count);
        Assert.Equal("a", subRows[1]["tag"]);
        Assert.Equal("b", subRows[2]["tag"]);
        var firstVector = Assert.IsAssignableFrom<ReadOnlyMemory<float>>(subRows[1]["vector"]);
        Assert.Equal(new[] { 1f, 2f }, firstVector.ToArray());

        await ResetCollectionAsync(collectionName);
    }

    [Fact]
    public async Task CreateSimpleCollection_builds_schema_index_and_loads()
    {
        const string collectionName = nameof(CreateSimpleCollection_builds_schema_index_and_loads);

        await ResetCollectionAsync(collectionName);

        // The simple create takes just a name and dimension; everything else defaults (id/vector, INT64 pk,
        // COSINE + AUTOINDEX, dynamic fields on).
        await Client.CreateCollectionAsync(new CreateSimpleCollectionReq
        {
            CollectionName = collectionName,
            Dimension = 2
        }, TestContext.Current.CancellationToken);

        // The collection should be created, loaded, and searchable via its default vector field.
        DescribeCollectionResp description = await Client.DescribeCollectionAsync(
            new DescribeCollectionReq { CollectionName = collectionName }, TestContext.Current.CancellationToken);
        Assert.Equal(collectionName, description.CollectionName);
        Assert.Equal(2, description.Schema.Fields.Count);
        Assert.Contains(description.Schema.Fields, f => f.Name == "id" && f.IsPrimaryKey);
        Assert.Contains(description.Schema.Fields, f => f.Name == "vector" && f.Dimension == 2);

        await WaitForLoadAsync(collectionName);
        GetLoadStateResp state = await Client.GetLoadStateAsync(
            new GetLoadStateReq { CollectionName = collectionName }, TestContext.Current.CancellationToken);
        Assert.Equal(LoadState.Loaded, state.State);

        await ResetCollectionAsync(collectionName);
    }
}
