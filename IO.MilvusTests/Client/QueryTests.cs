using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public class QueryTests : IClassFixture<QueryTests.QueryCollectionFixture>
{
    private string QueryCollectionName { get; }

    public QueryTests(QueryCollectionFixture queryCollectionFixture)
        => QueryCollectionName = queryCollectionFixture.CollectionName;

    [Fact]
    public async Task Query()
    {
        var queryResult = await Client.QueryAsync(
            QueryCollectionName,
            "id in [2, 3]",
            outputFields: new[] { "float_vector" });

        Assert.Equal(QueryCollectionName, queryResult.CollectionName);
        Assert.Equal(2, queryResult.FieldsData.Count);

        var idData = (Field<long>)Assert.Single(queryResult.FieldsData, d => d.FieldName == "id");
        Assert.Equal(MilvusDataType.Int64, idData.DataType);
        Assert.Equal(2, idData.RowCount);
        Assert.False(idData.IsDynamic);
        Assert.Collection(idData.Data,
            id => Assert.Equal(2, id),
            id => Assert.Equal(3, id));

        var floatVectorData =
            (FloatVectorField)Assert.Single(queryResult.FieldsData, d => d.FieldName == "float_vector");
        Assert.Equal(MilvusDataType.FloatVector, floatVectorData.DataType);
        Assert.Equal(2, floatVectorData.RowCount);
        Assert.False(floatVectorData.IsDynamic);
        Assert.Collection(floatVectorData.Data,
            v =>
            {
                Assert.Equal(3.5f, v.Span[0]);
                Assert.Equal(4.5f, v.Span[1]);
            },
            v =>
            {
                Assert.Equal(5f, v.Span[0]);
                Assert.Equal(6f, v.Span[1]);
            });
    }

    [Fact]
    public async Task Query_with_offset()
    {
        var queryResult = await Client.QueryAsync(
            QueryCollectionName,
            "id in [2, 3]",
            outputFields: new[] { "float_vector" },
            offset: 1,
            limit: 2);

        var idData = (Field<long>)Assert.Single(queryResult.FieldsData, d => d.FieldName == "id");
        Assert.Equal(1, idData.RowCount);
        Assert.Collection(idData.Data, id => Assert.Equal(3, id));
    }

    [Fact]
    public async Task Query_with_limit()
    {
        var queryResult = await Client.QueryAsync(
            QueryCollectionName,
            "id in [2, 3]",
            outputFields: new[] { "float_vector" },
            limit: 1);

        var idData = (Field<long>)Assert.Single(queryResult.FieldsData, d => d.FieldName == "id");
        Assert.Equal(1, idData.RowCount);
        Assert.Collection(idData.Data, id => Assert.Equal(2, id));
    }

    public class QueryCollectionFixture : IAsyncLifetime
    {
        public string CollectionName => "QueryCollection";

        public async Task InitializeAsync()
        {
            await TestEnvironment.Client.DropCollectionAsync(CollectionName);
            await TestEnvironment.Client.CreateCollectionAsync(
                CollectionName,
                new[]
                {
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.CreateVarchar("varchar", 256),
                    FieldSchema.CreateFloatVector("float_vector", 2)
                });

            await TestEnvironment.Client.CreateIndexAsync(
                CollectionName, "float_vector", MilvusIndexType.Flat,
                MilvusSimilarityMetricType.L2, new Dictionary<string, string>(), "float_vector_idx");

            long[] ids = { 1, 2, 3, 4, 5 };
            string[] strings = { "one", "two", "three", "four", "five" };
            ReadOnlyMemory<float>[] floatVectors =
            {
                new[] { 1f, 2f },
                new[] { 3.5f, 4.5f },
                new[] { 5f, 6f },
                new[] { 7.7f, 8.8f },
                new[] { 9f, 10f }
            };

            await TestEnvironment.Client.InsertAsync(
                CollectionName,
                new Field[]
                {
                    Field.Create("id", ids),
                    Field.Create("varchar", strings),
                    Field.CreateFloatVector("float_vector", floatVectors)
                });

            await TestEnvironment.Client.LoadCollectionAsync(CollectionName);
            await TestEnvironment.Client.WaitForCollectionLoadAsync(CollectionName,
                waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));
        }

        public Task DisposeAsync()
            => Task.CompletedTask;
    }

    private MilvusClient Client => TestEnvironment.Client;
}
