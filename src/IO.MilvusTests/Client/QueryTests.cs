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
            v => Assert.Equal(new List<float> { 3.5f, 4.5f }, v),
            v => Assert.Equal(new List<float> { 5f, 6f }, v));
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
                    FieldType.Create<long>("id", isPrimaryKey: true),
                    FieldType.CreateVarchar("varchar", 256),
                    FieldType.CreateFloatVector("float_vector", 2)
                });

            await TestEnvironment.Client.CreateIndexAsync(
                CollectionName, "float_vector", "float_vector_idx", MilvusIndexType.FLAT, MilvusMetricType.L2,
                new Dictionary<string, string>());

            var ids = new long[] { 1, 2, 3, 4, 5 };
            var strings = new[] { "one", "two", "three", "four", "five" };
            var floatVectors = new[]
            {
                new List<float> { 1f, 2f },
                new List<float> { 3.5f, 4.5f },
                new List<float> { 5f, 6f },
                new List<float> { 7.7f, 8.8f },
                new List<float> { 9f, 10f }
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
            await TestEnvironment.Client.WaitForCollectionLoadAsync(CollectionName, Array.Empty<string>(), TimeSpan.FromMilliseconds(100), TimeSpan.FromMinutes(1));
        }

        public Task DisposeAsync()
            => Task.CompletedTask;
    }

    private MilvusClient Client => TestEnvironment.Client;
}
