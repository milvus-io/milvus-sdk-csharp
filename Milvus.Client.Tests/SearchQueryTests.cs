using System.Text.Json;
using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class SearchQueryTests(
    MilvusFixture milvusFixture,
    SearchQueryTests.QueryCollectionFixture queryCollectionFixture)
    : IClassFixture<SearchQueryTests.QueryCollectionFixture>, IDisposable
{
    [Fact]
    public async Task Query()
    {
        var fields = await Collection.QueryAsync(
            "id in [2, 3]",
            new()
            {
                OutputFields = { "float_vector" },
                ConsistencyLevel = ConsistencyLevel.Strong
            });

        Assert.Equal(2, fields.Count);

        var idData = (FieldData<long>)Assert.Single(fields, d => d.FieldName == "id");
        Assert.Equal(MilvusDataType.Int64, idData.DataType);
        Assert.Equal(2, idData.RowCount);
        Assert.False(idData.IsDynamic);
        Assert.Collection(idData.Data,
            id => Assert.Equal(2, id),
            id => Assert.Equal(3, id));

        var floatVectorData =
            (FloatVectorFieldData)Assert.Single(fields, d => d.FieldName == "float_vector");
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
        var results = await Collection.QueryAsync(
            "id in [2, 3]",
            new()
            {
                OutputFields = { "float_vector" },
                Limit = 2,
                Offset = 1,
                ConsistencyLevel = ConsistencyLevel.Strong
            });

        var idData = (FieldData<long>)Assert.Single(results, d => d.FieldName == "id");
        Assert.Equal(1, idData.RowCount);
        Assert.Collection(idData.Data, id => Assert.Equal(3, id));
    }

    [Fact]
    public async Task Query_with_limit()
    {
        var results = await Collection.QueryAsync(
            "id in [2, 3]",
            new()
            {
                OutputFields = { "float_vector" },
                Limit = 1,
                ConsistencyLevel = ConsistencyLevel.Strong
            });

        var idData = (FieldData<long>)Assert.Single(results, d => d.FieldName == "id");
        Assert.Equal(1, idData.RowCount);
        Assert.Collection(idData.Data, id => Assert.Equal(2, id));
    }

    [Fact]
    public async Task Query_dynamic_field()
    {
        await Collection.DropAsync();

        await Client.CreateCollectionAsync(
            Collection.Name,
            new CollectionSchema
            {
                Fields =
                {
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("float_vector", 2)
                },
                EnableDynamicFields = true
            });

        await Collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2, "float_vector_idx", new Dictionary<string, string>());

        await Collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new[] { 1L, 2L, 3L }),
                FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
                {
                    new[] { 1f, 2f },
                    new[] { 3f, 4f },
                    new[] { 5f, 6f }
                }),
                FieldData.CreateVarChar("dynamic_varchar", new[] { "str1", "str2", "str3" }, isDynamic: true),
                FieldData.Create("dynamic_long", new[] { 8L, 9L, 10L }, isDynamic: true),
                FieldData.Create("dynamic_bool", new[] { true, false, true }, isDynamic: true)
            });

        await Collection.LoadAsync();
        await Collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        IReadOnlyList<FieldData> fields = await Collection.QueryAsync(
            "dynamic_long > 8",
            new QueryParameters { OutputFields = { "*" } });

        var idData = (FieldData<long>)Assert.Single(fields, d => d.FieldName == "id");
        Assert.Equal(MilvusDataType.Int64, idData.DataType);
        Assert.Equal(2, idData.RowCount);
        Assert.False(idData.IsDynamic);
        Assert.Collection(idData.Data,
            id => Assert.Equal(2, id),
            id => Assert.Equal(3, id));

        var dynamicVarcharData = (FieldData<string>)Assert.Single(fields, d => d.FieldName == "dynamic_varchar");
        Assert.Equal(MilvusDataType.VarChar, dynamicVarcharData.DataType);
        Assert.Equal(2, dynamicVarcharData.RowCount);
        Assert.True(dynamicVarcharData.IsDynamic);
        Assert.Collection(dynamicVarcharData.Data,
            s => Assert.Equal("str2", s),
            s => Assert.Equal("str3", s));

        var dynamicLongData = (FieldData<long>)Assert.Single(fields, d => d.FieldName == "dynamic_long");
        Assert.Equal(MilvusDataType.Int64, dynamicLongData.DataType);
        Assert.Equal(2, dynamicLongData.RowCount);
        Assert.True(dynamicVarcharData.IsDynamic);
        Assert.Collection(dynamicLongData.Data,
            i => Assert.Equal(9L, i),
            i => Assert.Equal(10L, i));

        var dynamicBoolData = (FieldData<bool>)Assert.Single(fields, d => d.FieldName == "dynamic_bool");
        Assert.Equal(MilvusDataType.Bool, dynamicBoolData.DataType);
        Assert.Equal(2, dynamicBoolData.RowCount);
        Assert.True(dynamicBoolData.IsDynamic);
        Assert.Collection(dynamicBoolData.Data,
            Assert.False,
            Assert.True);
    }

    [Fact]
    public async Task Search_with_minimal_inputs()
    {
        var results = await Collection.SearchAsync(
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            SimilarityMetricType.L2,
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
        var results = await Collection.SearchAsync(
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            SimilarityMetricType.L2,
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
        var results = await Collection.SearchAsync(
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            SimilarityMetricType.L2,
            limit: 2,
            new() { Offset = 1 });

        Assert.Equal(CollectionName, results.CollectionName);
        Assert.Empty(results.FieldsData);
        Assert.Collection(results.Ids.LongIds!.Order(),
            id => Assert.Equal(2, id),
            id => Assert.Equal(3, id));
        Assert.Null(results.Ids.StringIds); // TODO: Need to test string IDs
        Assert.Equal(1, results.NumQueries);
        Assert.Equal(2, results.Scores.Count);
        Assert.Equal(2, results.Limit);
        Assert.Collection(results.Limits, l => Assert.Equal(2, l));
    }

    [Fact]
    public async Task Search_with_range_search()
    {
        var results = await Collection.SearchAsync(
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            SimilarityMetricType.L2,
            limit: 5,
            new()
            {
                ExtraParameters =
                {
                    { "radius", "60" },
                    { "range_filter", "10" }
                }
            });

        Assert.Collection(
            results.Ids.LongIds!.Order(),
            id => Assert.Equal(2, id),
            id => Assert.Equal(3, id));
    }

    [Fact]
    public async Task Search_with_no_results()
    {
        // Create and load an empty collection
        MilvusCollection collection = Client.GetCollection(nameof(Search_with_no_results));
        string collectionName = collection.Name;

        await collection.DropAsync();
        collection = await Client.CreateCollectionAsync(
            collectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });

        await collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        var results = await collection.SearchAsync(
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            SimilarityMetricType.L2,
            limit: 2,
            new() { OutputFields = { "id" } });

        Assert.Equal(collectionName, results.CollectionName);

        // When there are no results, Milvus returns a null "Ids" result, so there's no way to know if it's generally
        // long or string IDs
        Assert.Null(results.Ids.LongIds);
        Assert.Null(results.Ids.StringIds);

        if (await Client.GetParsedMilvusVersion() < new Version(2, 5))
        {
            Assert.Empty(results.FieldsData);
        }
        else
        {
            Assert.All(results.FieldsData, f => Assert.Equal(0, f.RowCount));
        }

        Assert.Equal(1, results.NumQueries);
        Assert.Empty(results.Scores);
    }

    [Fact]
    public async Task Search_with_json_filter()
    {
        var results = await Collection.SearchAsync(
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            SimilarityMetricType.L2,
            limit: 2,
            new() { Expression = """json_thing["Number"] > 2""" });

        Assert.Equal(CollectionName, results.CollectionName);
        Assert.Empty(results.FieldsData);
        Assert.Collection(results.Ids.LongIds!.Order(),
            id => Assert.Equal(3, id),
            id => Assert.Equal(4, id));
        Assert.Null(results.Ids.StringIds);
        Assert.Equal(1, results.NumQueries);
        Assert.Equal(2, results.Scores.Count);
        Assert.Equal(2, results.Limit);
        Assert.Collection(results.Limits, l => Assert.Equal(2, l));
    }

    [Fact]
    public async Task Search_with_dynamic_field_filter()
    {
        await Collection.DropAsync();

        await Client.CreateCollectionAsync(
            Collection.Name,
            new CollectionSchema
            {
                Fields =
                {
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("float_vector", 2)
                },
                EnableDynamicFields = true
            });

        await Collection.CreateIndexAsync(
            "float_vector", IndexType.Flat, SimilarityMetricType.L2, "float_vector_idx", new Dictionary<string, string>());

        await Collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new[] { 1L, 2L, 3L }),
                FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
                {
                    new[] { 1f, 2f },
                    new[] { 3f, 4f },
                    new[] { 5f, 6f }
                }),
                FieldData.CreateVarChar("dynamic_varchar", new[] { "str1", "str2", "str3" }, isDynamic: true),
                FieldData.Create("dynamic_long", new[] { 8L, 9L, 10L }, isDynamic: true),
                FieldData.Create("dynamic_bool", new[] { true, false, true }, isDynamic: true)
            });

        await Collection.LoadAsync();
        await Collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        var results = await Collection.SearchAsync(
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 3f, 4f } },
            SimilarityMetricType.L2,
            limit: 2,
            new()
            {
                Expression = """dynamic_long > 8""",
                OutputFields = { "*" }
            });

        var fields = results.FieldsData;

        var idData = (FieldData<long>)Assert.Single(fields, d => d.FieldName == "id");
        Assert.Equal(MilvusDataType.Int64, idData.DataType);
        Assert.Equal(2, idData.RowCount);
        Assert.False(idData.IsDynamic);
        Assert.Collection(idData.Data,
            id => Assert.Equal(2, id),
            id => Assert.Equal(3, id));

        var dynamicVarcharData = (FieldData<string>)Assert.Single(fields, d => d.FieldName == "dynamic_varchar");
        Assert.Equal(MilvusDataType.VarChar, dynamicVarcharData.DataType);
        Assert.Equal(2, dynamicVarcharData.RowCount);
        Assert.True(dynamicVarcharData.IsDynamic);
        Assert.Collection(dynamicVarcharData.Data,
            s => Assert.Equal("str2", s),
            s => Assert.Equal("str3", s));

        var dynamicLongData = (FieldData<long>)Assert.Single(fields, d => d.FieldName == "dynamic_long");
        Assert.Equal(MilvusDataType.Int64, dynamicLongData.DataType);
        Assert.Equal(2, dynamicLongData.RowCount);
        Assert.True(dynamicVarcharData.IsDynamic);
        Assert.Collection(dynamicLongData.Data,
            i => Assert.Equal(9L, i),
            i => Assert.Equal(10L, i));

        var dynamicBoolData = (FieldData<bool>)Assert.Single(fields, d => d.FieldName == "dynamic_bool");
        Assert.Equal(MilvusDataType.Bool, dynamicBoolData.DataType);
        Assert.Equal(2, dynamicBoolData.RowCount);
        Assert.True(dynamicBoolData.IsDynamic);
        Assert.Collection(dynamicBoolData.Data,
            Assert.False,
            Assert.True);
    }

    [Theory]
    [InlineData(IndexType.BinFlat, SimilarityMetricType.Jaccard)]
    [InlineData(IndexType.BinFlat, SimilarityMetricType.Hamming)]
    [InlineData(IndexType.BinIvfFlat, SimilarityMetricType.Hamming)]
    public async Task Search_binary_vector(IndexType indexType, SimilarityMetricType similarityMetricType)
    {
        MilvusCollection binaryVectorCollection = Client.GetCollection(nameof(Search_binary_vector));
        string collectionName = binaryVectorCollection.Name;

        await binaryVectorCollection.DropAsync();
        await Client.CreateCollectionAsync(
            collectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateBinaryVector("binary_vector", 128)
            });

        await binaryVectorCollection.CreateIndexAsync(
            "binary_vector", indexType, similarityMetricType,
            "float_vector_idx", new Dictionary<string, string>() { { "nlist", "128" } });

        long[] ids = { 1, 2, 3 };
        string[] strings = { "one", "two", "three" };
        ReadOnlyMemory<byte>[] binaryVectors =
        {
            Enumerable.Range(1, 16).Select(i => (byte)i).ToArray(),
            Enumerable.Range(2, 16).Select(i => (byte)i).ToArray(),
            Enumerable.Range(3, 16).Select(i => (byte)i).ToArray(),
        };

        await binaryVectorCollection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", ids),
                FieldData.Create("varchar", strings),
                FieldData.CreateBinaryVectors("binary_vector", binaryVectors)
            });

        await binaryVectorCollection.LoadAsync();
        await binaryVectorCollection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        var results = await binaryVectorCollection.SearchAsync(
            "binary_vector",
            new[] { binaryVectors[0] },
            similarityMetricType,
            limit: 2,
            parameters: new SearchParameters
            {
                OutputFields = { "binary_vector" },
                ConsistencyLevel = ConsistencyLevel.Strong,
            });

        Assert.Equal(collectionName, results.CollectionName);

        var binaryVectorField = (BinaryVectorFieldData)Assert.Single(results.FieldsData);
        Assert.Equal((ReadOnlyMemory<byte>)new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }, binaryVectorField.Data[0]);
        Assert.Equal((ReadOnlyMemory<byte>)new byte[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 }, binaryVectorField.Data[1]);

        Assert.Collection(results.Ids.LongIds!,
            id => Assert.Equal(1, id),
            id => Assert.Equal(3, id));
        Assert.Null(results.Ids.StringIds);
        Assert.Equal(1, results.NumQueries);
        Assert.Equal(2, results.Scores.Count);
        Assert.Equal(2, results.Limit);
        Assert.Collection(results.Limits, l => Assert.Equal(2, l));
    }

#if NET8_0_OR_GREATER
    [Theory]
    [InlineData(IndexType.Flat, SimilarityMetricType.L2)]
    [InlineData(IndexType.Flat, SimilarityMetricType.Ip)]
    [InlineData(IndexType.Hnsw, SimilarityMetricType.L2)]
    public async Task Search_float16_vector(IndexType indexType, SimilarityMetricType similarityMetricType)
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        MilvusCollection float16VectorCollection = Client.GetCollection(nameof(Search_float16_vector));
        string collectionName = float16VectorCollection.Name;

        await float16VectorCollection.DropAsync();
        await Client.CreateCollectionAsync(
            collectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateFloat16Vector("float16_vector", 128)
            });

        var indexParams = indexType == IndexType.Hnsw
            ? new Dictionary<string, string> { ["M"] = "16", ["efConstruction"] = "200" }
            : new Dictionary<string, string> { ["nlist"] = "128" };

        await float16VectorCollection.CreateIndexAsync(
            "float16_vector", indexType, similarityMetricType,
            "float16_vector_idx", indexParams);

        long[] ids = { 1, 2, 3 };
        string[] strings = { "one", "two", "three" };
        ReadOnlyMemory<Half>[] float16Vectors =
        {
            Enumerable.Range(0, 128).Select(i => (Half)(i * 1.0f)).ToArray(),
            Enumerable.Range(0, 128).Select(i => (Half)(i * 10.0f)).ToArray(),
            Enumerable.Range(0, 128).Select(i => (Half)(i * 1.1f)).ToArray(),
        };

        await float16VectorCollection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", ids),
                FieldData.Create("varchar", strings),
                FieldData.CreateFloat16Vector("float16_vector", float16Vectors)
            });

        await float16VectorCollection.LoadAsync();
        await float16VectorCollection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        var results = await float16VectorCollection.SearchAsync(
            "float16_vector",
            new[] { float16Vectors[0] },
            similarityMetricType,
            limit: 2,
            parameters: new() { ConsistencyLevel = ConsistencyLevel.Strong });

        Assert.Equal(collectionName, results.CollectionName);

        Assert.Empty(results.FieldsData);
        Assert.Equal(2, results.Ids.LongIds!.Count);
        Assert.All(results.Ids.LongIds, id => Assert.InRange(id, 1, 3));
        Assert.Null(results.Ids.StringIds);
        Assert.Equal(1, results.NumQueries);
        Assert.Equal(2, results.Scores.Count);
        Assert.Equal(2, results.Limit);
        Assert.Collection(results.Limits, l => Assert.Equal(2, l));
    }
#endif

    [Fact(Skip = "Milvus returns 'only support to travel back to 0s so far, but got 1s'")]
    public async Task Query_with_time_travel()
    {
        // This tests inserts row, takes a timestamp, inserts another row, and then queries using the timestamp for
        // time travel. Only the first row should be returned.

        MilvusCollection collection = Client.GetCollection(nameof(Query_with_time_travel));
        string collectionName = collection.Name;

        await collection.DropAsync();
        collection = await Client.CreateCollectionAsync(
            collectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });

        await collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);

        await collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new long[] { 1 }),
                FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[] { new[] { 1f, 2f } })
            });

        var timestamp = DateTime.UtcNow;

        await collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new long[] { 2 }),
                FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[] { new[] { 3f, 4f } })
            });

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        // Query without time travel
        var results = await collection.QueryAsync("id > 0");
        var idData = (FieldData<long>)Assert.Single(results, d => d.FieldName == "id");
        Assert.Collection(idData.Data,
            id => Assert.Equal(1, id),
            id => Assert.Equal(2, id));

        // Query with time travel
        results = await collection.QueryAsync(
            "id > 0",
            new() { TimeTravelTimestamp = MilvusTimestampUtils.FromDateTime(timestamp) });
        idData = (FieldData<long>)Assert.Single(results, d => d.FieldName == "id");
        Assert.Collection(idData.Data, id => Assert.Equal(1, id));
    }

    [Fact]
    public async Task Search_with_all_supported_types()
    {
        var collection = Client.GetCollection("all_types");
        await collection.DropAsync();
        await Client.CreateCollectionAsync(
            collection.Name,
            new[]
            {
                FieldSchema.Create<long>("key", isPrimaryKey: true),
                FieldSchema.Create<bool>("bool"),
                FieldSchema.Create<sbyte>("int8"),
                FieldSchema.Create<short>("int16"),
                FieldSchema.Create<int>("int32"),
                FieldSchema.Create<long>("int64"),
                FieldSchema.Create<float>("float"),
                FieldSchema.Create<double>("double"),
                FieldSchema.CreateVarchar("varchar", maxLength: 10),
                FieldSchema.CreateArray<bool>("bool_array", maxCapacity: 2),
                FieldSchema.CreateArray<sbyte>("int8_array", maxCapacity: 2),
                FieldSchema.CreateArray<short>("int16_array", maxCapacity: 2),
                FieldSchema.CreateArray<int>("int32_array", maxCapacity: 2),
                FieldSchema.CreateArray<long>("int64_array", maxCapacity: 2),
                FieldSchema.CreateArray<float>("float_array", maxCapacity: 2),
                FieldSchema.CreateArray<double>("double_array", maxCapacity: 2),
                FieldSchema.CreateVarcharArray("varchar_array", maxCapacity: 2, maxLength: 10),
                FieldSchema.CreateJson("json"),
                FieldSchema.CreateFloatVector("float_vector", dimension: 2),
            });

        await collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);

        await collection.InsertAsync(
            new[]
            {
                FieldData.Create("key", new[] { 1L, 2L }),
                FieldData.Create("bool", new[] { false, true }),
                FieldData.Create("int8", new sbyte[] { 1, 2 }),
                FieldData.Create("int16", new short[] { 1, 2 }),
                FieldData.Create("int32", new[] { 1, 2 }),
                FieldData.Create("int64", new[] { 1L, 2L }),
                FieldData.Create("float", new[] { 1.1f, 2.2f }),
                FieldData.Create("double", new[] { 1.1, 2.2 }),
                FieldData.CreateVarChar("varchar", new[] { "one", "two" }),
                FieldData.CreateArray("bool_array", new[] { new[] { true }, new bool[] { } }),
                FieldData.CreateArray("int8_array", new[] { new sbyte[] { 1 }, new sbyte[] { } }),
                FieldData.CreateArray("int16_array", new[] { new short[] { 1 }, new short[] { } }),
                FieldData.CreateArray("int32_array", new[] { new[] { 1 }, new int[] { } }),
                FieldData.CreateArray("int64_array", new[] { new[] { 1L }, new long[] { } }),
                FieldData.CreateArray("float_array", new[] { new[] { 1.1f }, new float[] { } }),
                FieldData.CreateArray("double_array", new[] { new[] { 1.1 }, new double[] { } }),
                FieldData.CreateArray("varchar_array", new[] { new[] { "one" }, new string[] { } }),
                FieldData.CreateJson("json", new[] { "{}", "{\"a\":1}" }),
                FieldData.CreateFloatVector("float_vector", new[]
                {
                    (ReadOnlyMemory<float>)new[] { 1.1f, 2.2f },
                    (ReadOnlyMemory<float>)new[] { 3.3f, 4.4f },
                }),
            });

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        var results = await collection.SearchAsync(
            "float_vector",
            new[] { (ReadOnlyMemory<float>)new[] { 1.1f, 2.2f } },
            SimilarityMetricType.L2,
            2,
            new SearchParameters
            {
                OutputFields =
                {
                    "bool",
                    "int8",
                    "int16",
                    "int32",
                    "int64",
                    "float",
                    "double",
                    "varchar",
                    "bool_array",
                    "int8_array",
                    "int16_array",
                    "int32_array",
                    "int64_array",
                    "float_array",
                    "double_array",
                    "varchar_array",
                    "json",
                    "float_vector",
                },
            });

        Assert.Equal(18, results.FieldsData.Count);

        var boolField = (FieldData<bool>)results.FieldsData.Single(f => f.FieldName == "bool");
        Assert.Equal(new[] { false, true }, boolField.Data);
        var int8Field = (FieldData<int>)results.FieldsData.Single(f => f.FieldName == "int8");
        Assert.Equal(new[] { 1, 2 }, int8Field.Data);
        var int16Field = (FieldData<int>)results.FieldsData.Single(f => f.FieldName == "int16");
        Assert.Equal(new[] { 1, 2 }, int16Field.Data);
        var int32Field = (FieldData<int>)results.FieldsData.Single(f => f.FieldName == "int32");
        Assert.Equal(new[] { 1, 2 }, int32Field.Data);
        var int64Field = (FieldData<long>)results.FieldsData.Single(f => f.FieldName == "int64");
        Assert.Equal(new[] { 1L, 2L }, int64Field.Data);
        var floatField = (FieldData<float>)results.FieldsData.Single(f => f.FieldName == "float");
        Assert.Equal(new[] { 1.1f, 2.2f }, floatField.Data);
        var doubleField = (FieldData<double>)results.FieldsData.Single(f => f.FieldName == "double");
        Assert.Equal(new[] { 1.1, 2.2 }, doubleField.Data);
        var varcharField = (FieldData<string>)results.FieldsData.Single(f => f.FieldName == "varchar");
        Assert.Equal(new[] { "one", "two" }, varcharField.Data);
        var boolArrayField = (ArrayFieldData<bool>)results.FieldsData.Single(f => f.FieldName == "bool_array");
        Assert.Equal(new[] { new[] { true }, new bool[] { } }, boolArrayField.Data);
        var int8ArrayField = (ArrayFieldData<int>)results.FieldsData.Single(f => f.FieldName == "int8_array");
        Assert.Equal(new[] { new[] { 1 }, new int[] { } }, int8ArrayField.Data);
        var int16ArrayField = (ArrayFieldData<int>)results.FieldsData.Single(f => f.FieldName == "int16_array");
        Assert.Equal(new[] { new[] { 1 }, new int[] { } }, int16ArrayField.Data);
        var int32ArrayField = (ArrayFieldData<int>)results.FieldsData.Single(f => f.FieldName == "int32_array");
        Assert.Equal(new[] { new[] { 1 }, new int[] { } }, int32ArrayField.Data);
        var int64ArrayField = (ArrayFieldData<long>)results.FieldsData.Single(f => f.FieldName == "int64_array");
        Assert.Equal(new[] { new[] { 1L }, new long[] { } }, int64ArrayField.Data);
        var floatArrayField = (ArrayFieldData<float>)results.FieldsData.Single(f => f.FieldName == "float_array");
        Assert.Equal(new[] { new[] { 1.1f }, new float[] { } }, floatArrayField.Data);
        var doubleArrayField = (ArrayFieldData<double>)results.FieldsData.Single(f => f.FieldName == "double_array");
        Assert.Equal(new[] { new[] { 1.1 }, new double[] { } }, doubleArrayField.Data);
        var varcharArrayField = (ArrayFieldData<string>)results.FieldsData.Single(f => f.FieldName == "varchar_array");
        Assert.Equal(new[] { new[] { "one" }, new string[] { } }, varcharArrayField.Data);
        var jsonField = (FieldData<string>)results.FieldsData.Single(f => f.FieldName == "json");
        Assert.Equal(new[] { "{}", "{\"a\":1}" }, jsonField.Data);
        var floatVectorField = (FloatVectorFieldData)results.FieldsData.Single(f => f.FieldName == "float_vector");
        Assert.Equal((ReadOnlyMemory<float>)new[] { 1.1f, 2.2f }, floatVectorField.Data[0]);
        Assert.Equal((ReadOnlyMemory<float>)new[] { 3.3f, 4.4f }, floatVectorField.Data[1]);
    }

    [Fact]
    public Task Search_with_wrong_metric_type_throws()
        => Assert.ThrowsAsync<MilvusException>(() => Collection.SearchAsync(
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            SimilarityMetricType.Ip,
            limit: 2));

    [Fact]
    public async Task Search_with_unsupported_vector_type_throws()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => Collection.SearchAsync(
            "float_vector",
            new ReadOnlyMemory<string>[] { new[] { "foo", "bar" } },
            SimilarityMetricType.Ip,
            limit: 2));

        Assert.Equal("vectors", exception.ParamName);
    }

    [Fact]
    public async Task Search_with_group_by_field()
    {
        var parameters = new SearchParameters();
        parameters.GroupByField = "id";
        var results = await Collection.SearchAsync(
            "float_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            SimilarityMetricType.L2,
            parameters: parameters,
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
    public async Task Search_sparse_vector()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        MilvusCollection sparseCollection = Client.GetCollection(nameof(Search_sparse_vector));
        string collectionName = sparseCollection.Name;

        await sparseCollection.DropAsync();
        await Client.CreateCollectionAsync(
            collectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateSparseFloatVector("sparse_vector"),
            });

        var sparseVectors = new[]
        {
            new MilvusSparseVector<float>(new Dictionary<int, float> { [0] = 1.0f, [1] = 2.0f }),
            new MilvusSparseVector<float>(new Dictionary<int, float> { [0] = 10.0f, [1] = 20.0f }),
            new MilvusSparseVector<float>(new Dictionary<int, float> { [2] = 5.0f, [3] = 6.0f }),
        };

        await sparseCollection.InsertAsync(new FieldData[]
        {
            FieldData.Create("id", new[] { 1L, 2L, 3L }),
            FieldData.CreateSparseFloatVector("sparse_vector", sparseVectors),
        });

        await sparseCollection.CreateIndexAsync(
            "sparse_vector",
            IndexType.SparseInvertedIndex,
            SimilarityMetricType.Ip);

        await sparseCollection.LoadAsync();
        await sparseCollection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100),
            timeout: TimeSpan.FromMinutes(1));

        var queryVector = new MilvusSparseVector<float>(new Dictionary<int, float> { [0] = 1.0f, [1] = 1.0f });

        var searchResults = await sparseCollection.SearchAsync(
            "sparse_vector",
            new[] { queryVector },
            SimilarityMetricType.Ip,
            limit: 2,
            new SearchParameters { ConsistencyLevel = ConsistencyLevel.Strong });

        Assert.Equal(collectionName, searchResults.CollectionName);
        Assert.NotNull(searchResults.Ids.LongIds);
        Assert.Equal(2, searchResults.Ids.LongIds.Count);
        Assert.Equal(2L, searchResults.Ids.LongIds[0]);
        Assert.Equal(1L, searchResults.Ids.LongIds[1]);
    }

    [Fact]
    public async Task Query_sparse_vector()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        MilvusCollection sparseCollection = Client.GetCollection(nameof(Query_sparse_vector));
        string collectionName = sparseCollection.Name;

        await sparseCollection.DropAsync();
        await Client.CreateCollectionAsync(
            collectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateSparseFloatVector("sparse_vector"),
            });

        var sparseVectors = new[]
        {
            new MilvusSparseVector<float>(new Dictionary<int, float> { [0] = 1.0f, [100] = 2.0f }),
            new MilvusSparseVector<float>(new Dictionary<int, float> { [50] = 3.0f, [200] = 4.0f }),
        };

        await sparseCollection.InsertAsync(new FieldData[]
        {
            FieldData.Create("id", new[] { 1L, 2L }),
            FieldData.CreateSparseFloatVector("sparse_vector", sparseVectors),
        });

        await sparseCollection.CreateIndexAsync(
            "sparse_vector",
            IndexType.SparseInvertedIndex,
            SimilarityMetricType.Ip);

        await sparseCollection.LoadAsync();
        await sparseCollection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100),
            timeout: TimeSpan.FromMinutes(1));

        var results = await sparseCollection.QueryAsync(
            "id > 0",
            new QueryParameters
            {
                OutputFields = { "sparse_vector" },
                ConsistencyLevel = ConsistencyLevel.Strong
            });

        Assert.Equal(2, results.Count);

        var idData = (FieldData<long>)results.First(f => f.FieldName == "id");
        Assert.Equal(2, idData.RowCount);

        var sparseVectorData = (SparseFloatVectorFieldData)results.First(f => f.FieldName == "sparse_vector");
        Assert.Equal(2, sparseVectorData.RowCount);
        Assert.Equal(MilvusDataType.SparseFloatVector, sparseVectorData.DataType);

        Assert.Equal(new[] { 0, 100 }, sparseVectorData.Data[0].Indices);
        Assert.Equal(new[] { 1.0f, 2.0f }, sparseVectorData.Data[0].Values);

        Assert.Equal(new[] { 50, 200 }, sparseVectorData.Data[1].Indices);
        Assert.Equal(new[] { 3.0f, 4.0f }, sparseVectorData.Data[1].Values);
    }

    public class QueryCollectionFixture : IAsyncLifetime
    {
        public QueryCollectionFixture(MilvusFixture milvusFixture)
        {
            Client = milvusFixture.CreateClient();
            Collection = Client.GetCollection(nameof(SearchQueryTests));
        }

        private readonly MilvusClient Client;
        public readonly MilvusCollection Collection;

        public async Task InitializeAsync()
        {
            await Collection.DropAsync();
            await Client.CreateCollectionAsync(
                Collection.Name,
                new[]
                {
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.CreateVarchar("varchar", 256),
                    FieldSchema.CreateJson("json_thing"),
                    FieldSchema.CreateFloatVector("float_vector", 2)
                });

            await Collection.CreateIndexAsync(
                "float_vector", IndexType.Flat, SimilarityMetricType.L2, "float_vector_idx", new Dictionary<string, string>());

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

            List<string> jsons = Enumerable.Range(1, 5)
                .Select(i => JsonSerializer.Serialize(new JsonThing { Title = "Title" + i, Number = i }))
                .ToList();

            await Collection.InsertAsync(
                new[]
                {
                    FieldData.Create("id", ids),
                    FieldData.Create("varchar", strings),
                    FieldData.CreateJson("json_thing", jsons),
                    FieldData.CreateFloatVector("float_vector", floatVectors)
                });

            await Collection.LoadAsync();
            await Collection.WaitForCollectionLoadAsync(
                waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));
        }

        public Task DisposeAsync()
        {
            Client.Dispose();
            return Task.CompletedTask;
        }
    }

    private readonly MilvusClient Client = milvusFixture.CreateClient();

    private MilvusCollection Collection => queryCollectionFixture.Collection;
    private string CollectionName => Collection.Name;

    public void Dispose() => Client.Dispose();

    internal class JsonThing
    {
        public string? Title { get; set; }
        public int Number { get; set; }
    }
}
