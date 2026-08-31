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
    public async Task Row_insert_rejects_empty_input()
    {
        MilvusCollection collection = Client.GetCollection(nameof(Row_insert_rejects_empty_input));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            collection.InsertAsync(
                new List<IDictionary<string, object?>>(),
                cancellationToken: TestContext.Current.CancellationToken));
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
