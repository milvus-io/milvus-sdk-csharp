using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class SearchWithIteratorLongKeyTests : IAsyncLifetime
{
    [Fact]
    public async Task SearchWithIterator_basic()
    {
        await Collection.InsertAsync(
        [
            FieldData.Create("id", new[] { 1L, 2L, 3L, 4L, 5L }),
            FieldData.CreateVarChar("varchar", new[] { "a", "b", "c", "d", "e" }),
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

        Assert.True(results.Count == 3);
        Assert.True(results[0].Ids.LongIds!.Count == 2);
        Assert.True(results[1].Ids.LongIds!.Count == 2);
        Assert.True(results[2].Ids.LongIds!.Count == 1);

        int totalResults = results.Sum(r => r.Ids.LongIds!.Count);
        Assert.Equal(5, totalResults);
    }

    [Fact]
    public async Task SearchWithIterator_with_output_fields()
    {
        await Collection.InsertAsync(
        [
            FieldData.Create("id", new[] { 1L, 2L, 3L }),
            FieldData.CreateVarChar("varchar", new[] { "text1", "text2", "text3" }),
            FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
            {
                new[] { 1f, 2f, 3f, 4f },
                new[] { 2f, 3f, 4f, 5f },
                new[] { 3f, 4f, 5f, 6f }
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
            limit: 3,
            batchSize: 2,
            parameters: new SearchParameters { OutputFields = { "varchar" } });

        List<SearchResults> results = new();
        await foreach (var result in iterator)
        {
            results.Add(result);
        }

        Assert.Equal(2, results.Count);
        Assert.NotEmpty(results[0].FieldsData);
        Assert.Contains(results[0].FieldsData, f => f.FieldName == "varchar");
    }

    [Fact]
    public async Task SearchWithIterator_with_expression()
    {
        await Collection.InsertAsync(
        [
            FieldData.Create("id", new[] { 1L, 2L, 3L, 4L, 5L }),
            FieldData.CreateVarChar("varchar", new[] { "a", "b", "c", "d", "e" }),
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
            batchSize: 2,
            parameters: new SearchParameters { Expression = "id > 2" });

        List<SearchResults> results = new();
        await foreach (var result in iterator)
        {
            results.Add(result);
        }

        int totalResults = results.Sum(r => r.Ids.LongIds!.Count);
        Assert.True(totalResults > 0);
        Assert.True(totalResults <= 5);

        var allIds = results.SelectMany(r => r.Ids.LongIds!).ToList();
        Assert.All(allIds, id => Assert.True(id > 2));
    }

    [Fact]
    public async Task SearchWithIterator_offset_not_supported()
    {
        await Collection.InsertAsync(
        [
            FieldData.Create("id", new[] { 1L }),
            FieldData.CreateVarChar("varchar", new[] { "test" }),
            FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[] { new[] { 1f, 2f, 3f, 4f } })
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
            parameters: new SearchParameters { Offset = 1 });

        await Assert.ThrowsAsync<MilvusException>(async () =>
        {
            await foreach (var _ in iterator)
            {
            }
        });
    }

    [Fact]
    public async Task SearchWithIterator_limit_reached()
    {
        await Collection.InsertAsync(
        [
            FieldData.Create("id", Enumerable.Range(1, 100).Select(i => (long)i).ToArray()),
            FieldData.CreateVarChar("varchar", Enumerable.Range(1, 100).Select(i => $"text{i}").ToArray()),
            FieldData.CreateFloatVector("float_vector",
                Enumerable.Range(1, 100).Select(i => new ReadOnlyMemory<float>(new float[] { i, i, i, i })).ToArray())
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
            limit: 10,
            batchSize: 3);

        List<SearchResults> results = new();
        await foreach (var result in iterator)
        {
            results.Add(result);
        }

        int totalResults = results.Sum(r => r.Ids.LongIds!.Count);
        Assert.Equal(10, totalResults);
    }

    [Fact]
    public async Task SearchWithIterator_multiple_query_vectors()
    {
        await Collection.InsertAsync(
        [
            FieldData.Create("id", new[] { 1L, 2L, 3L, 4L, 5L, 6L }),
            FieldData.CreateVarChar("varchar", new[] { "a", "b", "c", "d", "e", "f" }),
            FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
            {
                new[] { 1f, 0f, 0f, 0f },
                new[] { 0f, 1f, 0f, 0f },
                new[] { 0f, 0f, 1f, 0f },
                new[] { 0f, 0f, 0f, 1f },
                new[] { 1f, 1f, 0f, 0f },
                new[] { 0f, 0f, 1f, 1f }
            })
        ]);
        await Collection.FlushAsync();

        await Collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);
        await Collection.WaitForIndexBuildAsync("float_vector");
        await Collection.LoadAsync();

        var queryVectors = new ReadOnlyMemory<float>[]
        {
            new[] { 1f, 0f, 0f, 0f },
            new[] { 0f, 1f, 0f, 0f }
        };

        var iterator = Collection.SearchWithIteratorAsync(
            "float_vector",
            queryVectors,
            SimilarityMetricType.L2,
            limit: 3,
            batchSize: 2);

        List<SearchResults> results = new();
        await foreach (var result in iterator)
        {
            results.Add(result);
        }

        Assert.NotEmpty(results);
        Assert.True(results[0].NumQueries == 2);
    }

    public async Task InitializeAsync()
    {
        await Collection.DropAsync();
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateFloatVector("float_vector", 4),
            });
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    private const string CollectionName = nameof(SearchWithIteratorLongKeyTests);

    private readonly MilvusClient Client;

    private MilvusCollection Collection { get; }

    public SearchWithIteratorLongKeyTests(MilvusFixture milvusFixture)
    {
        Client = milvusFixture.CreateClient();
        Collection = Client.GetCollection(CollectionName);
    }
}
