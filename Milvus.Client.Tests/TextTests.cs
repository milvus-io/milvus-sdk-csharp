using Xunit;

namespace Milvus.Client.Tests;

// Root-caused via a live container's own logs (docker logs, not just client-side symptoms): Milvus
// 2.6.4's streaming-node flusher unrecoverably panics -- crashing the entire server process, not just
// the request -- when it processes a collection containing a Text field with no max_length:
//
//   panic: new a empty data sync service should never be failed, the max_length was not specified,
//   field type is Text
//     .../flushcommon/pipeline.NewEmptyStreamingNodeDataSyncService(...)
//     .../flusherimpl.(*flusherComponents).WhenCreateCollection(...)
//     .../flusherimpl.(*WALFlusherImpl).dispatch(...)
//     created by .../flusherimpl.RecoverWALFlusher
//
// The call stack (RecoverWALFlusher) means this doesn't fire synchronously on CreateCollectionAsync --
// it fires whenever the flusher subsystem next recovers/rescans WAL channels, which can happen a good
// while after the offending collection was created (and even after it was dropped again, if the
// recovery pass catches it mid-window). That delay is why this looked like a "concurrency" issue in
// earlier investigation: the crash surfaces later, taking out whatever happens to be running at that
// point, not necessarily whatever created the bad collection. FieldSchema.CreateText requires
// maxLength for exactly this reason -- no code going through the public API can hit this -- so this
// class does not exercise the bad shape live; see FieldSchema.CreateText's XML doc for the client-side
// contract this protects. Kept out of the parallel pool as ordinary defense-in-depth, not because this
// class's own tests are known to trigger anything themselves.
[CollectionDefinition(nameof(TextTests), DisableParallelization = true)]
public sealed class TextTestsCollection;

[Collection(nameof(TextTests))]
public class TextTests : IAsyncLifetime
{
    private readonly MilvusClient Client;

    public TextTests(MilvusFixture milvusFixture) => Client = milvusFixture.CreateClient();

    [Fact]
    public async Task Insert_and_query_round_trip()
    {
        if (await Skip()) return;

        MilvusCollection collection = await CreateCollectionAsync(nameof(Insert_and_query_round_trip));

        await collection.CreateIndexAsync("vec", IndexType.Flat, SimilarityMetricType.L2, cancellationToken: TestContext.Current.CancellationToken);
        await collection.InsertAsync(new FieldData[]
        {
            FieldData.Create("id", new long[] { 1 }),
            FieldData.CreateText("content", new[] { "hello text world" }),
            FieldData.CreateFloatVector("vec", new ReadOnlyMemory<float>[] { new float[] { 1, 1, 1, 1 } }),
        }, cancellationToken: TestContext.Current.CancellationToken);
        await collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await collection.WaitForCollectionLoadAsync(cancellationToken: TestContext.Current.CancellationToken);

        var results = await collection.QueryAsync(
            "id == 1", new QueryParameters { OutputFields = { "content" } }, cancellationToken: TestContext.Current.CancellationToken);
        var field = (FieldData<string?>)Assert.Single(results, f => f.FieldName == "content");
        Assert.Equal("hello text world", Assert.Single(field.Data));

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Describe_round_trips_properties()
    {
        if (await Skip()) return;

        MilvusCollection collection = await CreateCollectionAsync(nameof(Describe_round_trips_properties));

        MilvusCollectionDescription description = await collection.DescribeAsync(TestContext.Current.CancellationToken);
        FieldSchema field = description.Schema.Fields.Single(f => f.Name == "content");

        Assert.Equal(MilvusDataType.Text, field.DataType);
        Assert.Equal(1000, field.MaxLength);
        Assert.True(field.EnableAnalyzer);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    // "Create collection without max_length fails" is deliberately not tested here: creating that exact
    // collection shape (a Text field with no max_length) is what triggers the server panic documented
    // above, so it runs against its own dedicated, disposable container instead of this shared one -- see
    // TextMaxLengthCrashRegressionTests.

    [Fact]
    public async Task Rejects_as_primary_key()
    {
        if (await Skip()) return;

        MilvusCollection collection = Client.GetCollection(nameof(Rejects_as_primary_key));
        await collection.DropAsync(TestContext.Current.CancellationToken);

        FieldSchema idField = FieldSchema.Create("id", MilvusDataType.Text, isPrimaryKey: true);
        idField.MaxLength = 100;

        await Assert.ThrowsAsync<MilvusException>(() =>
            Client.CreateCollectionAsync(
                nameof(Rejects_as_primary_key),
                new[]
                {
                    idField,
                    FieldSchema.CreateFloatVector("vec", 4),
                }, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Rejects_default_value()
    {
        if (await Skip()) return;

        MilvusCollection collection = Client.GetCollection(nameof(Rejects_default_value));
        await collection.DropAsync(TestContext.Current.CancellationToken);

        // FieldSchema.CreateText has no defaultValue parameter; go through the general overload to confirm
        // the server (not just the client) rejects it -- consistent with Milvus's documented "no default
        // values" restriction for Text fields. Milvus itself doesn't get a chance to weigh in here: our
        // own FieldSchema.ToGrpc-equivalent conversion rejects it client-side first.
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Client.CreateCollectionAsync(
                nameof(Rejects_default_value),
                new[]
                {
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.Create("content", MilvusDataType.Text, nullable: true, defaultValue: "fallback"),
                    FieldSchema.CreateFloatVector("vec", 4),
                }, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TEXT_MATCH_is_currently_rejected()
    {
        if (await Skip()) return;

        // Unlike VarChar (see TextMatchTests), Milvus rejects any filter expression -- TEXT_MATCH
        // included -- against a Text field outright, even with EnableMatch and EnableAnalyzer both set.
        // Confirmed against 2.6.4: "filter on text field (...) is not supported yet". This test fails
        // loudly if that ever changes, so the docs here don't go stale silently.
        MilvusCollection collection = Client.GetCollection(nameof(TEXT_MATCH_is_currently_rejected));
        await collection.DropAsync(TestContext.Current.CancellationToken);

        await Client.CreateCollectionAsync(
            nameof(TEXT_MATCH_is_currently_rejected),
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateText("content", 1000, enableAnalyzer: true, enableMatch: true),
                FieldSchema.CreateFloatVector("vec", 4),
            }, cancellationToken: TestContext.Current.CancellationToken);

        await collection.CreateIndexAsync("vec", IndexType.Flat, SimilarityMetricType.L2, cancellationToken: TestContext.Current.CancellationToken);
        await collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await collection.WaitForCollectionLoadAsync(cancellationToken: TestContext.Current.CancellationToken);

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            collection.QueryAsync("TEXT_MATCH(content, 'fox')", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Contains("not supported", exception.Message, StringComparison.OrdinalIgnoreCase);

        await collection.DropAsync(TestContext.Current.CancellationToken);
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
                FieldSchema.CreateText("content", 1000, enableAnalyzer: true),
                FieldSchema.CreateFloatVector("vec", 4),
            }, cancellationToken: TestContext.Current.CancellationToken);

        return collection;
    }

    private async Task<bool> Skip() => await Client.GetParsedMilvusVersion() < new Version(2, 6);

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }
}
