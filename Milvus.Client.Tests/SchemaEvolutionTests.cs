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
