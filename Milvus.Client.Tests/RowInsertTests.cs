using Xunit;

namespace Milvus.Client.Tests;

public class RowInsertTests(MilvusFixture milvusFixture) : IAsyncLifetime
{
    [Fact]
    public async Task Row_insert_matches_column_insert()
    {
        MilvusCollection collection = await CreateCollectionAsync(nameof(Row_insert_matches_column_insert));

        await collection.InsertAsync(
            new List<IDictionary<string, object?>>
            {
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["name"] = "one",
                    ["score"] = 1.5f,
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 1f, 0f })
                },
                new Dictionary<string, object?>
                {
                    ["id"] = 2L,
                    ["name"] = "two",
                    ["score"] = 2.5f,
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 0f, 1f })
                }
            }, cancellationToken: TestContext.Current.CancellationToken);

        await LoadAsync(collection);

        IReadOnlyList<FieldData> fields = await collection.QueryAsync(
            "id in [1, 2]",
            new QueryParameters
            {
                OutputFields = { "name", "score", "vector" },
                ConsistencyLevel = ConsistencyLevel.Strong
            }, TestContext.Current.CancellationToken);

        var ids = (FieldData<long>)Assert.Single(fields, f => f.FieldName == "id");
        Assert.Equal(new[] { 1L, 2L }, ids.Data.OrderBy(v => v));

        var names = (FieldData<string>)Assert.Single(fields, f => f.FieldName == "name");
        Assert.Equal(new[] { "one", "two" }, names.Data.OrderBy(v => v, StringComparer.Ordinal));

        var scores = (FieldData<float>)Assert.Single(fields, f => f.FieldName == "score");
        Assert.Equal(new[] { 1.5f, 2.5f }, scores.Data.OrderBy(v => v));

        var vectors = (FloatVectorFieldData)Assert.Single(fields, f => f.FieldName == "vector");
        Assert.Equal(2, vectors.RowCount);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Row_insert_widens_integer_types()
    {
        // The schema says Int64, but the rows carry int. The pivot must widen rather than reject:
        // a caller building dictionaries by hand will rarely bother with the L suffix.
        MilvusCollection collection = await CreateCollectionAsync(nameof(Row_insert_widens_integer_types));

        await collection.InsertAsync(
            new List<IDictionary<string, object?>>
            {
                new Dictionary<string, object?>
                {
                    ["id"] = 7,
                    ["name"] = "seven",
                    ["score"] = 1,
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 1f, 1f })
                }
            }, cancellationToken: TestContext.Current.CancellationToken);

        await LoadAsync(collection);

        IReadOnlyList<FieldData> fields = await collection.QueryAsync(
            "id == 7",
            new QueryParameters { ConsistencyLevel = ConsistencyLevel.Strong },
            TestContext.Current.CancellationToken);

        var ids = (FieldData<long>)Assert.Single(fields, f => f.FieldName == "id");
        Assert.Equal(7L, Assert.Single(ids.Data));

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Row_insert_with_dynamic_fields()
    {
        MilvusCollection collection = Client.GetCollection(nameof(Row_insert_with_dynamic_fields));
        await collection.DropAsync(TestContext.Current.CancellationToken);

        await Client.CreateCollectionAsync(
            collection.Name,
            new CollectionSchema
            {
                Fields =
                {
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("vector", 2)
                },
                EnableDynamicFields = true
            }, cancellationToken: TestContext.Current.CancellationToken);

        await collection.CreateIndexAsync(
            "vector", IndexType.Flat, SimilarityMetricType.L2, "vector_idx",
            new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        await collection.InsertAsync(
            new List<IDictionary<string, object?>>
            {
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                    ["extra_text"] = "hello",
                    ["extra_number"] = 42L
                }
            }, cancellationToken: TestContext.Current.CancellationToken);

        await LoadAsync(collection);

        IReadOnlyList<FieldData> fields = await collection.QueryAsync(
            "id == 1",
            new QueryParameters { OutputFields = { "*" }, ConsistencyLevel = ConsistencyLevel.Strong },
            TestContext.Current.CancellationToken);

        var text = (FieldData<string>)Assert.Single(fields, f => f.FieldName == "extra_text");
        Assert.Equal("hello", Assert.Single(text.Data));
        Assert.True(text.IsDynamic);

        var number = (FieldData<long>)Assert.Single(fields, f => f.FieldName == "extra_number");
        Assert.Equal(42L, Assert.Single(number.Data));

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Row_insert_rejects_unknown_field_without_dynamic_fields()
    {
        MilvusCollection collection =
            await CreateCollectionAsync(nameof(Row_insert_rejects_unknown_field_without_dynamic_fields));

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            collection.InsertAsync(
                new List<IDictionary<string, object?>>
                {
                    new Dictionary<string, object?>
                    {
                        ["id"] = 1L,
                        ["name"] = "one",
                        ["score"] = 1f,
                        ["vector"] = new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                        ["not_a_field"] = "boom"
                    }
                }, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("not_a_field", exception.Message);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Row_insert_reports_type_mismatch_with_field_name()
    {
        MilvusCollection collection =
            await CreateCollectionAsync(nameof(Row_insert_reports_type_mismatch_with_field_name));

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            collection.InsertAsync(
                new List<IDictionary<string, object?>>
                {
                    new Dictionary<string, object?>
                    {
                        ["id"] = 1L,
                        ["name"] = "one",
                        ["score"] = 1f,
                        ["vector"] = "not a vector"
                    }
                }, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("vector", exception.Message);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Row_upsert_replaces_existing_row()
    {
        MilvusCollection collection = await CreateCollectionAsync(nameof(Row_upsert_replaces_existing_row));

        await collection.InsertAsync(
            new List<IDictionary<string, object?>>
            {
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["name"] = "before",
                    ["score"] = 1f,
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 1f, 0f })
                }
            }, cancellationToken: TestContext.Current.CancellationToken);

        await LoadAsync(collection);

        await collection.UpsertAsync(
            new List<IDictionary<string, object?>>
            {
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["name"] = "after",
                    ["score"] = 9f,
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 0f, 1f })
                }
            }, cancellationToken: TestContext.Current.CancellationToken);

        IReadOnlyList<FieldData> fields = await collection.QueryAsync(
            "id == 1",
            new QueryParameters
            {
                OutputFields = { "name", "score" },
                ConsistencyLevel = ConsistencyLevel.Strong
            }, TestContext.Current.CancellationToken);

        // The row was replaced, not duplicated.
        var ids = (FieldData<long>)Assert.Single(fields, f => f.FieldName == "id");
        Assert.Equal(1L, Assert.Single(ids.Data));

        var names = (FieldData<string>)Assert.Single(fields, f => f.FieldName == "name");
        Assert.Equal("after", Assert.Single(names.Data));

        var scores = (FieldData<float>)Assert.Single(fields, f => f.FieldName == "score");
        Assert.Equal(9f, Assert.Single(scores.Data));

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Row_upsert_inserts_when_key_absent()
    {
        MilvusCollection collection = await CreateCollectionAsync(nameof(Row_upsert_inserts_when_key_absent));

        await collection.UpsertAsync(
            new List<IDictionary<string, object?>>
            {
                new Dictionary<string, object?>
                {
                    ["id"] = 42L,
                    ["name"] = "fresh",
                    ["score"] = 3f,
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 1f, 1f })
                }
            }, cancellationToken: TestContext.Current.CancellationToken);

        await LoadAsync(collection);

        IReadOnlyList<FieldData> fields = await collection.QueryAsync(
            "id == 42",
            new QueryParameters { OutputFields = { "name" }, ConsistencyLevel = ConsistencyLevel.Strong },
            TestContext.Current.CancellationToken);

        var names = (FieldData<string>)Assert.Single(fields, f => f.FieldName == "name");
        Assert.Equal("fresh", Assert.Single(names.Data));

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Row_upsert_rejects_empty_input()
    {
        MilvusCollection collection = Client.GetCollection(nameof(Row_upsert_rejects_empty_input));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            collection.UpsertAsync(
                new List<IDictionary<string, object?>>(),
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Row_insert_rejects_empty_input()
    {
        MilvusCollection collection = Client.GetCollection(nameof(Row_insert_rejects_empty_input));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            collection.InsertAsync(
                new List<IDictionary<string, object?>>(),
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Row_insert_sends_null_for_nullable_fields()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 5))
        {
            return;
        }

        MilvusCollection collection = Client.GetCollection(nameof(Row_insert_sends_null_for_nullable_fields));
        await collection.DropAsync(TestContext.Current.CancellationToken);

        await Client.CreateCollectionAsync(
            collection.Name,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.Create<long?>("opt_int"),
                FieldSchema.Create<double?>("opt_double"),
                FieldSchema.CreateVarchar("opt_text", 32, nullable: true),
                FieldSchema.CreateFloatVector("vector", 2)
            }, cancellationToken: TestContext.Current.CancellationToken);

        await collection.CreateIndexAsync(
            "vector", IndexType.Flat, SimilarityMetricType.L2, "vector_idx",
            new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        // Row 1 carries values, row 2 sets them to null explicitly, row 3 omits them entirely. All
        // three shapes have to survive the pivot -- previously the numeric columns threw.
        await collection.InsertAsync(
            new List<IDictionary<string, object?>>
            {
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["opt_int"] = 7L,
                    ["opt_double"] = 1.5,
                    ["opt_text"] = "here",
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 1f, 0f })
                },
                new Dictionary<string, object?>
                {
                    ["id"] = 2L,
                    ["opt_int"] = null,
                    ["opt_double"] = null,
                    ["opt_text"] = null,
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 0f, 1f })
                },
                new Dictionary<string, object?>
                {
                    ["id"] = 3L,
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 1f, 1f })
                }
            }, cancellationToken: TestContext.Current.CancellationToken);

        await LoadAsync(collection);

        Assert.Equal(7L, await ReadNullableAsync<long>(collection, 1, "opt_int"));
        Assert.Equal(1.5, await ReadNullableAsync<double>(collection, 1, "opt_double"));

        Assert.Null(await ReadNullableAsync<long>(collection, 2, "opt_int"));
        Assert.Null(await ReadNullableAsync<double>(collection, 2, "opt_double"));

        Assert.Null(await ReadNullableAsync<long>(collection, 3, "opt_int"));
        Assert.Null(await ReadNullableAsync<double>(collection, 3, "opt_double"));

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Row_insert_rejects_null_for_a_non_nullable_field()
    {
        MilvusCollection collection =
            await CreateCollectionAsync(nameof(Row_insert_rejects_null_for_a_non_nullable_field));

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            collection.InsertAsync(
                new List<IDictionary<string, object?>>
                {
                    new Dictionary<string, object?>
                    {
                        ["id"] = 1L,
                        ["name"] = "one",
                        ["score"] = 1f,
                        ["vector"] = new ReadOnlyMemory<float>(new[] { 1f, 0f })
                    },
                    new Dictionary<string, object?>
                    {
                        ["id"] = 2L,
                        ["name"] = "two",
                        ["score"] = null,
                        ["vector"] = new ReadOnlyMemory<float>(new[] { 0f, 1f })
                    }
                }, cancellationToken: TestContext.Current.CancellationToken));

        // The message has to name both the field and which row is at fault.
        Assert.Contains("score", exception.Message);
        Assert.Contains("not nullable", exception.Message);
        Assert.Contains("row 1", exception.Message);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Row_insert_rejects_an_out_of_range_narrowing_value()
    {
        MilvusCollection collection =
            Client.GetCollection(nameof(Row_insert_rejects_an_out_of_range_narrowing_value));
        await collection.DropAsync(TestContext.Current.CancellationToken);

        await Client.CreateCollectionAsync(
            collection.Name,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.Create<sbyte>("small"),
                FieldSchema.CreateFloatVector("vector", 2)
            }, cancellationToken: TestContext.Current.CancellationToken);

        // 300 does not fit in an Int8. An unchecked cast would have silently stored 44.
        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            collection.InsertAsync(
                new List<IDictionary<string, object?>>
                {
                    new Dictionary<string, object?>
                    {
                        ["id"] = 1L,
                        ["small"] = 300,
                        ["vector"] = new ReadOnlyMemory<float>(new[] { 1f, 0f })
                    }
                }, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("small", exception.Message);
        Assert.Contains("300", exception.Message);
        Assert.Contains("row 0", exception.Message);

        // In-range values still go through, including the boundaries.
        await collection.InsertAsync(
            new List<IDictionary<string, object?>>
            {
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["small"] = 127,
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 1f, 0f })
                },
                new Dictionary<string, object?>
                {
                    ["id"] = 2L,
                    ["small"] = -128,
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 0f, 1f })
                }
            }, cancellationToken: TestContext.Current.CancellationToken);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Row_insert_allows_a_dynamic_field_missing_from_some_rows()
    {
        MilvusCollection collection =
            Client.GetCollection(nameof(Row_insert_allows_a_dynamic_field_missing_from_some_rows));
        await collection.DropAsync(TestContext.Current.CancellationToken);

        await Client.CreateCollectionAsync(
            collection.Name,
            new CollectionSchema
            {
                Fields =
                {
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("vector", 2)
                },
                EnableDynamicFields = true
            }, cancellationToken: TestContext.Current.CancellationToken);

        await collection.CreateIndexAsync(
            "vector", IndexType.Flat, SimilarityMetricType.L2, "vector_idx",
            new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        // Dynamic fields are per-row in Milvus, so a numeric key present in one row and absent from
        // another must not fail the batch. The string case always worked; the numeric one threw.
        await collection.InsertAsync(
            new List<IDictionary<string, object?>>
            {
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                    ["extra_number"] = 42L,
                    ["extra_text"] = "present"
                },
                new Dictionary<string, object?>
                {
                    ["id"] = 2L,
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 0f, 1f })
                }
            }, cancellationToken: TestContext.Current.CancellationToken);

        await LoadAsync(collection);

        IReadOnlyList<FieldData> fields = await collection.QueryAsync(
            "id == 1",
            new QueryParameters { OutputFields = { "*" }, ConsistencyLevel = ConsistencyLevel.Strong },
            TestContext.Current.CancellationToken);

        Assert.Equal(42L, Assert.Single(((FieldData<long>)Assert.Single(
            fields, f => f.FieldName == "extra_number")).Data));

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Row_insert_infers_dynamic_fields_from_unsigned_values()
    {
        MilvusCollection collection =
            Client.GetCollection(nameof(Row_insert_infers_dynamic_fields_from_unsigned_values));
        await collection.DropAsync(TestContext.Current.CancellationToken);

        await Client.CreateCollectionAsync(
            collection.Name,
            new CollectionSchema
            {
                Fields =
                {
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("vector", 2)
                },
                EnableDynamicFields = true
            }, cancellationToken: TestContext.Current.CancellationToken);

        await collection.CreateIndexAsync(
            "vector", IndexType.Flat, SimilarityMetricType.L2, "vector_idx",
            new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        // byte, ushort and uint convert fine, so type inference must accept them rather than calling
        // them unsupported.
        await collection.InsertAsync(
            new List<IDictionary<string, object?>>
            {
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["vector"] = new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                    ["from_byte"] = (byte)7,
                    ["from_ushort"] = (ushort)8,
                    ["from_uint"] = 9u
                }
            }, cancellationToken: TestContext.Current.CancellationToken);

        await LoadAsync(collection);

        IReadOnlyList<FieldData> fields = await collection.QueryAsync(
            "id == 1",
            new QueryParameters { OutputFields = { "*" }, ConsistencyLevel = ConsistencyLevel.Strong },
            TestContext.Current.CancellationToken);

        Assert.Equal(7L, Assert.Single(((FieldData<long>)Assert.Single(
            fields, f => f.FieldName == "from_byte")).Data));
        Assert.Equal(8L, Assert.Single(((FieldData<long>)Assert.Single(
            fields, f => f.FieldName == "from_ushort")).Data));
        Assert.Equal(9L, Assert.Single(((FieldData<long>)Assert.Single(
            fields, f => f.FieldName == "from_uint")).Data));

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    private async Task<T?> ReadNullableAsync<T>(MilvusCollection collection, long id, string fieldName)
        where T : struct
    {
        IReadOnlyList<FieldData> fields = await collection.QueryAsync(
            $"id == {id}",
            new QueryParameters
            {
                OutputFields = { fieldName },
                ConsistencyLevel = ConsistencyLevel.Strong
            }, TestContext.Current.CancellationToken);

        return Assert.Single(((FieldData<T?>)Assert.Single(fields, f => f.FieldName == fieldName)).Data);
    }

    private async Task<MilvusCollection> CreateCollectionAsync(string name)
    {
        MilvusCollection collection = Client.GetCollection(name);
        await collection.DropAsync(TestContext.Current.CancellationToken);

        await Client.CreateCollectionAsync(
            name,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("name", 128),
                FieldSchema.Create<float>("score"),
                FieldSchema.CreateFloatVector("vector", 2)
            }, cancellationToken: TestContext.Current.CancellationToken);

        await collection.CreateIndexAsync(
            "vector", IndexType.Flat, SimilarityMetricType.L2, "vector_idx",
            new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        return collection;
    }

    private static async Task LoadAsync(MilvusCollection collection)
    {
        await collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1),
            cancellationToken: TestContext.Current.CancellationToken);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }

    private readonly MilvusClient Client = milvusFixture.CreateClient();
}
