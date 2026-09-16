using Xunit;

namespace Milvus.Client.Tests;

// See TextTests's TextTestsCollection comment for the actual root cause of the server crashes that
// motivated this: a Text field created without max_length, not concurrency. This class never creates
// that shape, so it isn't known to trigger anything itself -- kept out of the parallel pool as
// ordinary defense-in-depth alongside TextTests, not because of a finding specific to this class.
[CollectionDefinition(nameof(TextMatchTests), DisableParallelization = true)]
public sealed class TextMatchTestsCollection;

[Collection(nameof(TextMatchTests))]
public class TextMatchTests : IAsyncLifetime
{
    private readonly MilvusClient Client;

    public TextMatchTests(MilvusFixture milvusFixture) => Client = milvusFixture.CreateClient();

    [Fact]
    public async Task TEXT_MATCH_finds_matching_rows()
    {
        if (await Skip()) return;

        MilvusCollection collection = await CreateCollectionAsync(nameof(TEXT_MATCH_finds_matching_rows));

        await collection.CreateIndexAsync("vec", IndexType.Flat, SimilarityMetricType.L2, cancellationToken: TestContext.Current.CancellationToken);
        await collection.InsertAsync(new FieldData[]
        {
            FieldData.Create("id", new long[] { 1, 2 }),
            FieldData.CreateVarChar("tag", new[] { "quick brown", "slow lazy" }),
            FieldData.CreateFloatVector("vec", new ReadOnlyMemory<float>[] { new float[] { 1, 1, 1, 1 }, new float[] { 2, 2, 2, 2 } }),
        }, cancellationToken: TestContext.Current.CancellationToken);
        await collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await collection.WaitForCollectionLoadAsync(cancellationToken: TestContext.Current.CancellationToken);

        var results = await collection.QueryAsync(
            "TEXT_MATCH(tag, 'lazy')", new QueryParameters { OutputFields = { "id" } },
            cancellationToken: TestContext.Current.CancellationToken);
        var field = (FieldData<long>)Assert.Single(results, f => f.FieldName == "id");
        Assert.Equal(new long[] { 2 }, field.Data);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TEXT_MATCH_rejected_without_EnableMatch()
    {
        if (await Skip()) return;

        // Milvus 2.5.20 does not enforce this at all -- TEXT_MATCH works there even with EnableMatch
        // left false, as long as EnableAnalyzer is set. The strict rejection is 2.6+ behavior.
        if (await Client.GetParsedMilvusVersion() < new Version(2, 6))
        {
            return;
        }

        MilvusCollection collection = Client.GetCollection(nameof(TEXT_MATCH_rejected_without_EnableMatch));
        await collection.DropAsync(TestContext.Current.CancellationToken);

        // EnableAnalyzer alone is not enough -- TEXT_MATCH specifically requires EnableMatch too.
        await Client.CreateCollectionAsync(
            nameof(TEXT_MATCH_rejected_without_EnableMatch),
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("tag", 64, enableAnalyzer: true),
                FieldSchema.CreateFloatVector("vec", 4),
            }, cancellationToken: TestContext.Current.CancellationToken);

        await collection.CreateIndexAsync("vec", IndexType.Flat, SimilarityMetricType.L2, cancellationToken: TestContext.Current.CancellationToken);
        await collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await collection.WaitForCollectionLoadAsync(cancellationToken: TestContext.Current.CancellationToken);

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            collection.QueryAsync("TEXT_MATCH(tag, 'lazy')", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Contains("does not enable match", exception.Message, StringComparison.OrdinalIgnoreCase);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task EnableMatch_without_EnableAnalyzer_is_rejected_client_side()
    {
        // Purely client-side: EnableMatch's contract requires EnableAnalyzer, and FieldSchema.ToGrpc()
        // enforces that itself before any request is sent, rather than letting the server reject it --
        // so this throws synchronously and needs no live collection to actually be created.
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            Client.CreateCollectionAsync(
                nameof(EnableMatch_without_EnableAnalyzer_is_rejected_client_side),
                new[]
                {
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.CreateVarchar("tag", 64, enableMatch: true),
                    FieldSchema.CreateFloatVector("vec", 4),
                }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Contains("EnableAnalyzer", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Describe_round_trips_EnableMatch()
    {
        if (await Skip()) return;

        MilvusCollection collection = await CreateCollectionAsync(nameof(Describe_round_trips_EnableMatch));

        MilvusCollectionDescription description = await collection.DescribeAsync(TestContext.Current.CancellationToken);
        FieldSchema field = description.Schema.Fields.Single(f => f.Name == "tag");
        Assert.True(field.EnableMatch);
        Assert.True(field.EnableAnalyzer);

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
                FieldSchema.CreateVarchar("tag", 64, enableAnalyzer: true, enableMatch: true),
                FieldSchema.CreateFloatVector("vec", 4),
            }, cancellationToken: TestContext.Current.CancellationToken);

        return collection;
    }

    private async Task<bool> Skip() => await Client.GetParsedMilvusVersion() < new Version(2, 5);

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }
}
