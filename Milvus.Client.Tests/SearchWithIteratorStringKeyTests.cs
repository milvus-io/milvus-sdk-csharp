using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class SearchWithIteratorStringKeyTests : IAsyncLifetime
{
    [Fact]
    public async Task SearchWithIterator_string_primary_key_basic()
    {
        await Collection.InsertAsync(
        [
            FieldData.CreateVarChar("id", new[] { "a", "b", "c", "d", "e" }),
            FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
            {
                new[] { 1f, 2f, 3f, 4f },
                new[] { 2f, 3f, 4f, 5f },
                new[] { 3f, 4f, 5f, 6f },
                new[] { 4f, 5f, 6f, 7f },
                new[] { 5f, 6f, 7f, 8f }
            })
        ]);
        await Collection.FlushAsync();

        await Collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);
        await Collection.WaitForIndexBuildAsync("float_vector");
        await Collection.LoadAsync();

        var queryVector = new ReadOnlyMemory<float>[] { new[] { 1f, 2f, 3f, 4f } };
        var iterator = Collection.SearchWithIteratorAsync(
            "float_vector",
            queryVector,
            SimilarityMetricType.L2,
            limit: 5,
            batchSize: 2);

        List<SearchResults> results = new();
        await foreach (var result in iterator)
        {
            results.Add(result);
        }

        int totalResults = results.Sum(r => r.Ids.StringIds!.Count);
        Assert.Equal(5, totalResults);

        var allIds = results.SelectMany(r => r.Ids.StringIds!).ToList();
        Assert.Equal(5, allIds.Count);
        Assert.All(allIds, id => Assert.NotNull(id));
    }

    [Fact]
    public async Task SearchWithIterator_string_pk_with_expression()
    {
        await Collection.InsertAsync(
        [
            FieldData.CreateVarChar("id", new[] { "a", "b", "c", "d", "e", "f" }),
            FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
            {
                new[] { 1f, 2f, 3f, 4f },
                new[] { 2f, 3f, 4f, 5f },
                new[] { 3f, 4f, 5f, 6f },
                new[] { 4f, 5f, 6f, 7f },
                new[] { 5f, 6f, 7f, 8f },
                new[] { 6f, 7f, 8f, 9f }
            })
        ]);
        await Collection.FlushAsync();

        await Collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);
        await Collection.WaitForIndexBuildAsync("float_vector");
        await Collection.LoadAsync();

        var queryVector = new ReadOnlyMemory<float>[] { new[] { 1f, 2f, 3f, 4f } };
        var iterator = Collection.SearchWithIteratorAsync(
            "float_vector",
            queryVector,
            SimilarityMetricType.L2,
            limit: 6,
            batchSize: 2,
            parameters: new SearchParameters { Expression = "id > 'b'" });

        List<SearchResults> results = new();
        await foreach (var result in iterator)
        {
            results.Add(result);
        }

        var allIds = results.SelectMany(r => r.Ids.StringIds!).ToList();
        Assert.All(allIds, id => Assert.True(string.CompareOrdinal(id, "b") > 0));
    }

    public async Task InitializeAsync()
    {
        await Collection.DropAsync();
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.CreateVarchar("id", 64, isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 4),
            });
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    private const string CollectionName = nameof(SearchWithIteratorStringKeyTests);

    private readonly MilvusClient Client;

    private MilvusCollection Collection { get; }

    public SearchWithIteratorStringKeyTests(MilvusFixture milvusFixture)
    {
        Client = milvusFixture.CreateClient();
        Collection = Client.GetCollection(CollectionName);
    }
}
