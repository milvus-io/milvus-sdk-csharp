using Xunit;

namespace Milvus.Client.Tests;

public class SchemaEvolutionTests(MilvusFixture milvusFixture) : IAsyncLifetime
{
    [Fact]
    public async Task AddCollectionField_adds_a_nullable_field_to_an_empty_collection()
    {
        if (await Skip()) return;

        MilvusCollection collection =
            await CreateCollectionAsync(nameof(AddCollectionField_adds_a_nullable_field_to_an_empty_collection));

        await collection.AddCollectionFieldAsync(
            FieldSchema.Create<long?>("extra", nullable: true), TestContext.Current.CancellationToken);

        MilvusCollectionDescription description = await collection.DescribeAsync(TestContext.Current.CancellationToken);
        Assert.Contains(description.Schema.Fields, f => f.Name == "extra");

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AddCollectionField_requires_nullable()
    {
        if (await Skip()) return;

        MilvusCollection collection = await CreateCollectionAsync(nameof(AddCollectionField_requires_nullable));

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            collection.AddCollectionFieldAsync(
                FieldSchema.Create<long>("extra"), TestContext.Current.CancellationToken));

        Assert.Contains("nullable", exception.Message);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AddCollectionField_requires_nullable_even_with_a_default_value()
    {
        if (await Skip()) return;

        // A default value looks like it should be enough to reconcile old rows, but Milvus treats
        // nullable and defaultValue as independent requirements: this is rejected exactly like the
        // no-default case above, same error.
        MilvusCollection collection =
            await CreateCollectionAsync(nameof(AddCollectionField_requires_nullable_even_with_a_default_value));

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            collection.AddCollectionFieldAsync(
                FieldSchema.Create<long>("extra", defaultValue: 99L), TestContext.Current.CancellationToken));

        Assert.Contains("nullable", exception.Message);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AddCollectionField_backfills_null_for_existing_rows()
    {
        if (await Skip()) return;

        MilvusCollection collection =
            await CreateLoadedCollectionWithOneRowAsync(nameof(AddCollectionField_backfills_null_for_existing_rows));

        await collection.AddCollectionFieldAsync(
            FieldSchema.Create<long?>("extra", nullable: true), TestContext.Current.CancellationToken);

        var field = (FieldData<long?>)await QuerySingleFieldAsync(collection, "extra");
        Assert.Null(Assert.Single(field.Data));

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AddCollectionField_backfills_the_default_value_for_existing_and_new_rows()
    {
        if (await Skip()) return;

        MilvusCollection collection = await CreateLoadedCollectionWithOneRowAsync(
            nameof(AddCollectionField_backfills_the_default_value_for_existing_and_new_rows));

        await collection.AddCollectionFieldAsync(
            FieldSchema.Create<long?>("extra", nullable: true, defaultValue: 55L),
            TestContext.Current.CancellationToken);

        var existingRowField = (FieldData<long?>)await QuerySingleFieldAsync(collection, "extra", "id == 1");
        Assert.Equal(55L, Assert.Single(existingRowField.Data));

        // A row inserted after the field exists, which does not mention it, gets the same default --
        // same behavior as a field declared at collection-creation time.
        await collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new[] { 2L }),
                FieldData.CreateFloatVector("vector", new[] { new ReadOnlyMemory<float>(new[] { 0f, 1f }) })
            }, cancellationToken: TestContext.Current.CancellationToken);

        var newRowField = (FieldData<long?>)await QuerySingleFieldAsync(collection, "extra", "id == 2");
        Assert.Equal(55L, Assert.Single(newRowField.Data));

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AddCollectionField_rejects_a_vector_field()
    {
        if (await Skip()) return;

        MilvusCollection collection = await CreateCollectionAsync(nameof(AddCollectionField_rejects_a_vector_field));

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            collection.AddCollectionFieldAsync(
                FieldSchema.CreateFloatVector("extra_vector", 2), TestContext.Current.CancellationToken));

        Assert.Contains("vector", exception.Message);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AddCollectionField_rejects_a_duplicate_field_name()
    {
        if (await Skip()) return;

        MilvusCollection collection =
            await CreateCollectionAsync(nameof(AddCollectionField_rejects_a_duplicate_field_name));

        await Assert.ThrowsAsync<MilvusException>(() =>
            collection.AddCollectionFieldAsync(
                FieldSchema.Create<long?>("id", nullable: true), TestContext.Current.CancellationToken));

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AddCollectionField_throws_for_a_collection_that_does_not_exist()
    {
        if (await Skip()) return;

        MilvusCollection collection = Client.GetCollection("schema_evolution_tests_no_such_collection");

        await Assert.ThrowsAsync<MilvusException>(() =>
            collection.AddCollectionFieldAsync(
                FieldSchema.Create<long?>("extra", nullable: true), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AlterCollectionField_increases_varchar_max_length()
    {
        if (await Skip()) return;

        MilvusCollection collection = await CreateVarcharCollectionAsync(
            nameof(AlterCollectionField_increases_varchar_max_length), maxLength: 10);

        await collection.AlterCollectionFieldAsync(
            "text", new Dictionary<string, string> { ["max_length"] = "20" },
            cancellationToken: TestContext.Current.CancellationToken);

        MilvusCollectionDescription description = await collection.DescribeAsync(TestContext.Current.CancellationToken);
        Assert.Equal(20, description.Schema.Fields.Single(f => f.Name == "text").MaxLength);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AlterCollectionField_decreases_varchar_max_length_without_truncating_existing_data()
    {
        if (await Skip()) return;

        // Unlike a typical database, Milvus does not validate existing data against a shrunk
        // max_length: the limit only applies to future writes, so a string already longer than the
        // new limit survives untouched.
        MilvusCollection collection = await CreateVarcharCollectionAsync(
            nameof(AlterCollectionField_decreases_varchar_max_length_without_truncating_existing_data),
            maxLength: 20);

        await collection.CreateIndexAsync(
            "vector", IndexType.Flat, SimilarityMetricType.L2, "vector_idx",
            new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        const string longValue = "this is 18 chars!!";
        Assert.Equal(18, longValue.Length);

        await collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new[] { 1L }),
                FieldData.CreateVarChar("text", new[] { longValue }),
                FieldData.CreateFloatVector("vector", new[] { new ReadOnlyMemory<float>(new[] { 1f, 0f }) })
            }, cancellationToken: TestContext.Current.CancellationToken);

        await collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1),
            cancellationToken: TestContext.Current.CancellationToken);

        await collection.AlterCollectionFieldAsync(
            "text", new Dictionary<string, string> { ["max_length"] = "5" },
            cancellationToken: TestContext.Current.CancellationToken);

        var field = (FieldData<string>)await QuerySingleFieldAsync(collection, "text");
        Assert.Equal(longValue, Assert.Single(field.Data));

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AlterCollectionField_sets_and_deletes_a_property()
    {
        if (await Skip()) return;

        // mmap.enabled has no observable effect through this SDK's read path (it is a segment loading
        // hint, not part of FieldSchema), so this only verifies both calls are accepted.
        MilvusCollection collection =
            await CreateVarcharCollectionAsync(nameof(AlterCollectionField_sets_and_deletes_a_property), 10);

        await collection.AlterCollectionFieldAsync(
            "text", new Dictionary<string, string> { ["mmap.enabled"] = "true" },
            cancellationToken: TestContext.Current.CancellationToken);

        await collection.AlterCollectionFieldAsync(
            "text", deleteKeys: new[] { "mmap.enabled" },
            cancellationToken: TestContext.Current.CancellationToken);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AlterCollectionField_rejects_an_unrecognized_delete_key()
    {
        if (await Skip()) return;

        // Rejected because Milvus does not recognize the key name as a field property at all -- not
        // because it was never set on this field. A recognized key like mmap.enabled can be deleted
        // even when never set (see the sibling test), so this is a name allow-list, not presence.
        MilvusCollection collection =
            await CreateVarcharCollectionAsync(nameof(AlterCollectionField_rejects_an_unrecognized_delete_key), 10);

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            collection.AlterCollectionFieldAsync(
                "text", deleteKeys: new[] { "not_a_real_property" },
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("not_a_real_property", exception.Message);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AlterCollectionField_throws_for_a_field_that_does_not_exist()
    {
        if (await Skip()) return;

        MilvusCollection collection =
            await CreateVarcharCollectionAsync(nameof(AlterCollectionField_throws_for_a_field_that_does_not_exist), 10);

        await Assert.ThrowsAsync<MilvusException>(() =>
            collection.AlterCollectionFieldAsync(
                "no_such_field", new Dictionary<string, string> { ["mmap.enabled"] = "true" },
                cancellationToken: TestContext.Current.CancellationToken));

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AlterCollectionField_requires_properties_or_delete_keys()
    {
        if (await Skip()) return;

        MilvusCollection collection = Client.GetCollection(nameof(AlterCollectionField_requires_properties_or_delete_keys));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            collection.AlterCollectionFieldAsync("text", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AlterCollectionField_throws_for_a_collection_that_does_not_exist()
    {
        if (await Skip()) return;

        MilvusCollection collection = Client.GetCollection("schema_evolution_tests_no_such_collection_2");

        await Assert.ThrowsAsync<MilvusException>(() =>
            collection.AlterCollectionFieldAsync(
                "text", new Dictionary<string, string> { ["mmap.enabled"] = "true" },
                cancellationToken: TestContext.Current.CancellationToken));
    }

    private async Task<bool> Skip() => await Client.GetParsedMilvusVersion() < new Version(2, 6);

    private async Task<FieldData> QuerySingleFieldAsync(
        MilvusCollection collection, string fieldName, string filter = "id == 1")
    {
        IReadOnlyList<FieldData> fields = await collection.QueryAsync(
            filter,
            new QueryParameters { OutputFields = { fieldName }, ConsistencyLevel = ConsistencyLevel.Strong },
            TestContext.Current.CancellationToken);

        return Assert.Single(fields, f => f.FieldName == fieldName);
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
                FieldSchema.CreateFloatVector("vector", 2)
            }, cancellationToken: TestContext.Current.CancellationToken);

        return collection;
    }

    private async Task<MilvusCollection> CreateVarcharCollectionAsync(string name, int maxLength)
    {
        MilvusCollection collection = Client.GetCollection(name);
        await collection.DropAsync(TestContext.Current.CancellationToken);

        await Client.CreateCollectionAsync(
            name,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("text", maxLength),
                FieldSchema.CreateFloatVector("vector", 2)
            }, cancellationToken: TestContext.Current.CancellationToken);

        return collection;
    }

    private async Task<MilvusCollection> CreateLoadedCollectionWithOneRowAsync(string name)
    {
        MilvusCollection collection = await CreateCollectionAsync(name);

        await collection.CreateIndexAsync(
            "vector", IndexType.Flat, SimilarityMetricType.L2, "vector_idx",
            new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        await collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new[] { 1L }),
                FieldData.CreateFloatVector("vector", new[] { new ReadOnlyMemory<float>(new[] { 1f, 0f }) })
            }, cancellationToken: TestContext.Current.CancellationToken);

        await collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1),
            cancellationToken: TestContext.Current.CancellationToken);

        return collection;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }

    private readonly MilvusClient Client = milvusFixture.CreateClient();
}
