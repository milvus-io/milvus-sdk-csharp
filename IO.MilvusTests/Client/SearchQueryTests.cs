using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public class SearchQueryTests : IClassFixture<SearchQueryTests.QueryCollectionFixture>
{
    private string CollectionName { get; }

    public SearchQueryTests(QueryCollectionFixture queryCollectionFixture)
        => CollectionName = queryCollectionFixture.CollectionName;

    [Fact]
    public async Task Query()
    {
        var queryResult = await Client.QueryAsync(
            CollectionName,
            "id in [2, 3]",
            outputFields: new[] { "float_vector" },
            consistencyLevel: ConsistencyLevel.Strong);

        Assert.Equal(CollectionName, queryResult.CollectionName);
        Assert.Equal(2, queryResult.FieldsData.Count);

        var idData = (FieldData<long>)Assert.Single(queryResult.FieldsData, d => d.FieldName == "id");
        Assert.Equal(MilvusDataType.Int64, idData.DataType);
        Assert.Equal(2, idData.RowCount);
        Assert.False(idData.IsDynamic);
        Assert.Collection(idData.Data,
            id => Assert.Equal(2, id),
            id => Assert.Equal(3, id));

        var floatVectorData =
            (FloatVectorFieldData)Assert.Single(queryResult.FieldsData, d => d.FieldName == "float_vector");
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
            CollectionName,
            "id in [2, 3]",
            outputFields: new[] { "float_vector" },
            limit: 2, offset: 1,
            consistencyLevel: ConsistencyLevel.Strong);

        var idData = (FieldData<long>)Assert.Single(queryResult.FieldsData, d => d.FieldName == "id");
        Assert.Equal(1, idData.RowCount);
        Assert.Collection(idData.Data, id => Assert.Equal(3, id));
    }

    [Fact]
    public async Task Query_with_limit()
    {
        var queryResult = await Client.QueryAsync(
            CollectionName,
            "id in [2, 3]",
            outputFields: new[] { "float_vector" },
            limit: 1,
            consistencyLevel: ConsistencyLevel.Strong);

        var idData = (FieldData<long>)Assert.Single(queryResult.FieldsData, d => d.FieldName == "id");
        Assert.Equal(1, idData.RowCount);
        Assert.Collection(idData.Data, id => Assert.Equal(2, id));
    }

    [Fact]
    public async Task Search_with_minimal_inputs()
    {
        var results = await Client.SearchAsync(
            CollectionName,
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            MilvusSimilarityMetricType.L2,
            limit: 2);

        Assert.Equal(CollectionName, results.CollectionName);
        Assert.Empty(results.FieldsData);
        Assert.Collection(results.Ids.LongIds!,
            id => Assert.Equal(1, id),
            id => Assert.Equal(2, id));
        Assert.Null(results.Ids.StringIds); // TODO: Need to test string IDs
        Assert.Equal(1, results.NumQueries);
        Assert.Equal(2, results.Scores.Count);
        Assert.Equal(2, results.Limit);
        Assert.Collection(results.Limits, l => Assert.Equal(2, l));
    }

    [Fact]
    public async Task Search_with_OutputFields()
    {
        var results = await Client.SearchAsync(
            CollectionName,
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            MilvusSimilarityMetricType.L2,
            limit: 2,
            new()
            {
                ConsistencyLevel = ConsistencyLevel.Strong,
                OutputFields = { "id", "varchar" }
            });

        Assert.Collection(results.FieldsData.OrderBy(f => f.FieldName),
            field =>
            {
                var f = Assert.IsType<FieldData<long>>(field);
                Assert.Equal("id", f.FieldName);
                Assert.Equal(MilvusDataType.Int64, f.DataType);
                Assert.Equal(2, f.RowCount);
                Assert.False(f.IsDynamic);
                Assert.Collection(f.Data,
                    id => Assert.Equal(1, id),
                    id => Assert.Equal(2, id));
            },
            field =>
            {
                var f = Assert.IsType<FieldData<string>>(field);
                Assert.Equal("varchar", f.FieldName);
                Assert.Equal(MilvusDataType.VarChar, f.DataType);
                Assert.Equal(2, f.RowCount);
                Assert.False(f.IsDynamic);
                Assert.Collection(f.Data,
                    s => Assert.Equal("one", s),
                    s => Assert.Equal("two", s));
            });
    }

    [Fact]
    public async Task Search_with_offset()
    {
        var results = await Client.SearchAsync(
            CollectionName,
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            MilvusSimilarityMetricType.L2,
            limit: 2,
            new() { Offset = 1 });

        Assert.Equal(CollectionName, results.CollectionName);
        Assert.Empty(results.FieldsData);
        Assert.Collection(results.Ids.LongIds!,
            id => Assert.Equal(2, id),
            id => Assert.Equal(3, id));
        Assert.Null(results.Ids.StringIds); // TODO: Need to test string IDs
        Assert.Equal(1, results.NumQueries);
        Assert.Equal(2, results.Scores.Count);
        Assert.Equal(2, results.Limit);
        Assert.Collection(results.Limits, l => Assert.Equal(2, l));
    }

    [Fact(Skip = "This times out - just like pymilvus binary_example.py")]
    public async Task Search_binary_vector()
    {
        string collectionName = nameof(Search_binary_vector);
        await Client.DropCollectionAsync(collectionName);
        await Client.CreateCollectionAsync(
            collectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateBinaryVector("binary_vector", 128)
            });

        await Client.CreateIndexAsync(
            collectionName, "binary_vector", MilvusIndexType.BinFlat,
            MilvusSimilarityMetricType.Jaccard, new Dictionary<string, string>(), "float_vector_idx");

        long[] ids = { 1, 2, 3 };
        string[] strings = { "one", "two", "three" };
        ReadOnlyMemory<byte>[] binaryVectors =
        {
            Enumerable.Range(1, 16).Select(i => (byte)i).ToArray(),
            Enumerable.Range(2, 16).Select(i => (byte)i).ToArray(),
            Enumerable.Range(3, 16).Select(i => (byte)i).ToArray(),
        };

        await Client.InsertAsync(
            collectionName,
            new FieldData[]
            {
                FieldData.Create("id", ids),
                FieldData.Create("varchar", strings),
                FieldData.CreateBinaryVectors("binary_vector", binaryVectors)
            });

        await Client.LoadCollectionAsync(collectionName);
        await Client.WaitForCollectionLoadAsync(collectionName,
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        var results = await Client.SearchAsync(
            collectionName,
            "binary_vector",
            new ReadOnlyMemory<byte>[] { new byte[] { 2, 3 } },
            MilvusSimilarityMetricType.Jaccard,
            limit: 2,
            new() { ConsistencyLevel = ConsistencyLevel.Strong });

        Assert.Equal(collectionName, results.CollectionName);

        Assert.Empty(results.FieldsData);
        Assert.Collection(results.Ids.LongIds!,
            id => Assert.Equal(1, id),
            id => Assert.Equal(2, id));
        Assert.Null(results.Ids.StringIds);
        Assert.Equal(1, results.NumQueries);
        Assert.Equal(2, results.Scores.Count);
        Assert.Equal(2, results.Limit);
        Assert.Collection(results.Limits, l => Assert.Equal(2, l));
    }

    [Fact(Skip = "Milvus returns 'only support to travel back to 0s so far, but got 1s'")]
    public async Task Query_with_time_travel()
    {
        // This tests inserts row, takes a timestamp, inserts another row, and then queries using the timestamp for
        // time travel. Only the first row should be returned.

        string collectionName = nameof(Query_with_time_travel);
        await Client.DropCollectionAsync(collectionName);
        await Client.CreateCollectionAsync(
            collectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });

        await Client.CreateIndexAsync(
            collectionName, "float_vector", MilvusIndexType.Flat, MilvusSimilarityMetricType.L2);

        await Client.InsertAsync(
            collectionName,
            new FieldData[]
            {
                FieldData.Create("id", new long[] { 1 }),
                FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[] { new[] { 1f, 2f } })
            });

        var timestamp = DateTime.UtcNow;

        // Thread.Sleep(3000);
        await Client.InsertAsync(
            collectionName,
            new FieldData[]
            {
                FieldData.Create("id", new long[] { 2 }),
                FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[] { new[] { 3f, 4f } })
            });

        await Client.LoadCollectionAsync(collectionName);
        await Client.WaitForCollectionLoadAsync(collectionName,
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        // Query without time travel
        var results = await Client.QueryAsync(collectionName, "id > 0");
        var idData = (FieldData<long>)Assert.Single(results.FieldsData, d => d.FieldName == "id");
        Assert.Collection(idData.Data,
            id => Assert.Equal(1, id),
            id => Assert.Equal(2, id));

        // Query with time travel
        results = await Client.QueryAsync(collectionName, "id > 0", timeTravelTimestamp: MilvusTimestampUtils.FromDateTime(timestamp));
        idData = (FieldData<long>)Assert.Single(results.FieldsData, d => d.FieldName == "id");
        Assert.Collection(idData.Data, id => Assert.Equal(1, id));
    }

    [Fact]
    public Task Search_with_wrong_metric_type_throws()
        => Assert.ThrowsAsync<MilvusException>(() => Client.SearchAsync(
            CollectionName,
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            MilvusSimilarityMetricType.Ip,
            limit: 2));

    [Fact]
    public async Task Search_with_unsupported_vector_type_throws()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => Client.SearchAsync(
            CollectionName,
            "float_vector",
            new ReadOnlyMemory<string>[] { new[] { "foo", "bar" } },
            MilvusSimilarityMetricType.Ip,
            limit: 2));

        Assert.Equal("vectors", exception.ParamName);
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
                new FieldData[]
                {
                    FieldData.Create("id", ids),
                    FieldData.Create("varchar", strings),
                    FieldData.CreateFloatVector("float_vector", floatVectors)
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
