using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public class QueryTests : IClassFixture<QueryTests.QueryCollectionFixture>
{
    private string QueryCollectionName { get; }

    public QueryTests(QueryCollectionFixture queryCollectionFixture)
        => QueryCollectionName = queryCollectionFixture.CollectionName;

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Query(IMilvusClient client)
    {
        var queryResult = await client.QueryAsync(
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

    [Theory(Skip = "Fails")]
    [ClassData(typeof(TestClients))]
    public async Task Query_with_offset(IMilvusClient client)
    {
        var queryResult = await client.QueryAsync(
            QueryCollectionName,
            "id in [2, 3]",
            outputFields: new[] { "float_vector" },
            offset: 1);

        var idData = (Field<long>)Assert.Single(queryResult.FieldsData, d => d.FieldName == "id");
        Assert.Equal(1, idData.RowCount);
        Assert.Collection(idData.Data, id => Assert.Equal(3, id));
    }

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Query_with_limit(IMilvusClient client)
    {
        var queryResult = await client.QueryAsync(
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
            var config = MilvusConfig.Load().FirstOrDefault();
            if (config is null)
            {
                throw new InvalidOperationException("No client configs");
            }

            var client = config.CreateClient();

            await client.DropCollectionAsync(CollectionName);
            await client.CreateCollectionAsync(
                CollectionName,
                new[]
                {
                    FieldType.Create<long>("id", isPrimaryKey: true),
                    FieldType.CreateVarchar("varchar", 256),
                    FieldType.CreateFloatVector("float_vector", 2)
                });

            await client.CreateIndexAsync(
                CollectionName, "float_vector", "float_vector_idx", MilvusIndexType.FLAT, MilvusMetricType.L2);

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

            await client.InsertAsync(
                CollectionName,
                new Field[]
                {
                    Field.Create("id", ids),
                    Field.Create("varchar", strings),
                    Field.CreateFloatVector("float_vector", floatVectors)
                });

            await client.LoadCollectionAsync(CollectionName);
            await client.WaitForCollectionLoadAsync(CollectionName, Array.Empty<string>(), TimeSpan.FromMilliseconds(100), TimeSpan.FromMinutes(1));
        }

        public Task DisposeAsync()
            => Task.CompletedTask;
    }
}
