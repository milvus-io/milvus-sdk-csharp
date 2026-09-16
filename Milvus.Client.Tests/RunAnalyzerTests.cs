using Xunit;

namespace Milvus.Client.Tests;

// See TextTests's TextTestsCollection comment for the actual root cause of the server crashes that
// motivated this: a Text field created without max_length, not concurrency. This class never creates
// that shape, so it isn't known to trigger anything itself -- kept out of the parallel pool as
// ordinary defense-in-depth alongside TextTests, not because of a finding specific to this class.
[CollectionDefinition(nameof(RunAnalyzerTests), DisableParallelization = true)]
public sealed class RunAnalyzerTestsCollection;

[Collection(nameof(RunAnalyzerTests))]
public class RunAnalyzerTests : IAsyncLifetime
{
    private readonly MilvusClient Client;

    public RunAnalyzerTests(MilvusFixture milvusFixture) => Client = milvusFixture.CreateClient();

    [Fact]
    public async Task Tokenizes_with_explicit_analyzer_params()
    {
        if (await Skip()) return;

        var results = await Client.RunAnalyzerAsync(
            new[] { "The quick foxes are running" },
            new Dictionary<string, object> { ["type"] = "english" },
            cancellationToken: TestContext.Current.CancellationToken);

        var tokens = Assert.Single(results).Select(t => t.Token).ToList();
        // The "english" analyzer stems and drops stop words -- "the"/"are" disappear, "foxes"/"running"
        // are reduced to their stems.
        Assert.Equal(new[] { "quick", "fox", "run" }, tokens);
    }

    [Fact]
    public async Task Tokenizes_multiple_texts_in_order()
    {
        if (await Skip()) return;

        var results = await Client.RunAnalyzerAsync(
            new[] { "hello world", "goodbye world" },
            new Dictionary<string, object> { ["type"] = "standard" },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, results.Count);
        Assert.Equal(new[] { "hello", "world" }, results[0].Select(t => t.Token));
        Assert.Equal(new[] { "goodbye", "world" }, results[1].Select(t => t.Token));
    }

    [Fact]
    public async Task Uses_a_default_analyzer_when_no_params_are_given()
    {
        if (await Skip()) return;

        var results = await Client.RunAnalyzerAsync(
            new[] { "hello world" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "hello", "world" }, Assert.Single(results).Select(t => t.Token));
    }

    [Fact]
    public async Task WithDetail_populates_offsets_and_position()
    {
        if (await Skip()) return;

        var results = await Client.RunAnalyzerAsync(
            new[] { "The Quick Brown Fox" },
            new Dictionary<string, object> { ["type"] = "standard" },
            withDetail: true,
            cancellationToken: TestContext.Current.CancellationToken);

        AnalyzerToken[] tokens = Assert.Single(results).ToArray();
        Assert.Equal(4, tokens.Length);
        Assert.Equal(("the", 0, 3, 0), (tokens[0].Token, tokens[0].StartOffset, tokens[0].EndOffset, tokens[0].Position));
        Assert.Equal(("quick", 4, 9, 1), (tokens[1].Token, tokens[1].StartOffset, tokens[1].EndOffset, tokens[1].Position));
        Assert.Equal(("brown", 10, 15, 2), (tokens[2].Token, tokens[2].StartOffset, tokens[2].EndOffset, tokens[2].Position));
        Assert.Equal(("fox", 16, 19, 3), (tokens[3].Token, tokens[3].StartOffset, tokens[3].EndOffset, tokens[3].Position));
    }

    [Fact]
    public async Task Without_detail_offsets_and_position_come_back_as_zero()
    {
        if (await Skip()) return;

        var results = await Client.RunAnalyzerAsync(
            new[] { "The Quick Brown Fox" },
            new Dictionary<string, object> { ["type"] = "standard" },
            cancellationToken: TestContext.Current.CancellationToken);

        AnalyzerToken token = Assert.Single(results).First();
        Assert.Equal(0, token.StartOffset);
        Assert.Equal(0, token.EndOffset);
        Assert.Equal(0, token.Position);
    }

    [Fact]
    public async Task WithHash_populates_hash()
    {
        if (await Skip()) return;

        var withHash = await Client.RunAnalyzerAsync(
            new[] { "hello" }, new Dictionary<string, object> { ["type"] = "standard" },
            withHash: true, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(Assert.Single(Assert.Single(withHash)).Hash);

        var withoutHash = await Client.RunAnalyzerAsync(
            new[] { "hello" }, new Dictionary<string, object> { ["type"] = "standard" },
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(Assert.Single(Assert.Single(withoutHash)).Hash);
    }

    [Fact]
    public async Task Field_based_mode_works_for_a_bm25_input_field()
    {
        if (await Skip()) return;

        MilvusCollection collection = Client.GetCollection(nameof(Field_based_mode_works_for_a_bm25_input_field));
        await collection.DropAsync(TestContext.Current.CancellationToken);

        CollectionSchema schema = new();
        schema.Fields.Add(FieldSchema.Create<long>("id", isPrimaryKey: true));
        schema.Fields.Add(FieldSchema.CreateVarchar(
            "text", 256, enableAnalyzer: true, analyzerParams: new Dictionary<string, object> { ["type"] = "english" }));
        schema.Fields.Add(FieldSchema.CreateSparseFloatVector("sparse"));
        schema.Functions.Add(FunctionSchema.CreateBm25("bm25_fn", "text", "sparse"));
        await Client.CreateCollectionAsync(
            nameof(Field_based_mode_works_for_a_bm25_input_field), schema, cancellationToken: TestContext.Current.CancellationToken);

        await collection.CreateIndexAsync(
            "sparse", IndexType.SparseInvertedIndex, SimilarityMetricType.Bm25, cancellationToken: TestContext.Current.CancellationToken);
        await collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await collection.WaitForCollectionLoadAsync(cancellationToken: TestContext.Current.CancellationToken);

        var results = await Client.RunAnalyzerAsync(
            new[] { "The quick foxes are running" },
            collectionName: nameof(Field_based_mode_works_for_a_bm25_input_field),
            fieldName: "text",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "quick", "fox", "run" }, Assert.Single(results).Select(t => t.Token));

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Field_based_mode_rejects_a_field_that_is_not_a_bm25_input()
    {
        if (await Skip()) return;

        MilvusCollection collection = Client.GetCollection(nameof(Field_based_mode_rejects_a_field_that_is_not_a_bm25_input));
        await collection.DropAsync(TestContext.Current.CancellationToken);

        await Client.CreateCollectionAsync(
            nameof(Field_based_mode_rejects_a_field_that_is_not_a_bm25_input),
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("text", 256, enableAnalyzer: true),
                FieldSchema.CreateFloatVector("vec", 4),
            }, cancellationToken: TestContext.Current.CancellationToken);
        await collection.CreateIndexAsync("vec", IndexType.Flat, SimilarityMetricType.L2, cancellationToken: TestContext.Current.CancellationToken);
        await collection.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);
        await collection.WaitForCollectionLoadAsync(cancellationToken: TestContext.Current.CancellationToken);

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            Client.RunAnalyzerAsync(
                new[] { "hello world" },
                collectionName: nameof(Field_based_mode_rejects_a_field_that_is_not_a_bm25_input),
                fieldName: "text",
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Contains("bm25", exception.Message, StringComparison.OrdinalIgnoreCase);

        await collection.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Rejects_analyzerParams_together_with_collectionName_and_fieldName()
    {
        // Purely client-side validation -- no live server call is needed to hit this.
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            Client.RunAnalyzerAsync(
                new[] { "hello" },
                analyzerParams: new Dictionary<string, object> { ["type"] = "standard" },
                collectionName: "some_collection",
                fieldName: "some_field",
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Contains("mutually exclusive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("some_collection", null)]
    [InlineData(null, "some_field")]
    public async Task Rejects_only_one_of_collectionName_and_fieldName(string? collectionName, string? fieldName)
    {
        // Purely client-side validation -- no live server call is needed to hit this.
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            Client.RunAnalyzerAsync(
                new[] { "hello" },
                collectionName: collectionName,
                fieldName: fieldName,
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Contains("must be supplied together", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> Skip() => await Client.GetParsedMilvusVersion() < new Version(2, 5);

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }
}
