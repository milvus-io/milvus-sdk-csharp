using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class QueryWithIteratorExtendedTests : IAsyncLifetime
{
    [Fact]
    public async Task QueryWithIterator_string_pk_with_offset()
    {
        await Collection.DropAsync();
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.CreateVarchar("id", 64, isPrimaryKey: true),
                FieldSchema.CreateVarchar("text", 256),
                FieldSchema.CreateFloatVector("float_vector", 4),
            });

        var stringCollection = Client.GetCollection(CollectionName);

        await stringCollection.InsertAsync(
        [
            FieldData.CreateVarChar("id", new[] { "a", "b", "c", "d", "e", "f", "g", "h" }),
            FieldData.CreateVarChar("text", new[] { "text1", "text2", "text3", "text4", "text5", "text6", "text7", "text8" }),
            FieldData.CreateFloatVector("float_vector", new ReadOnlyMemory<float>[]
            {
                new[] { 1f, 0f, 0f, 0f },
                new[] { 2f, 0f, 0f, 0f },
                new[] { 3f, 0f, 0f, 0f },
                new[] { 4f, 0f, 0f, 0f },
                new[] { 5f, 0f, 0f, 0f },
                new[] { 6f, 0f, 0f, 0f },
                new[] { 7f, 0f, 0f, 0f },
                new[] { 8f, 0f, 0f, 0f }
            })
        ]);
        await stringCollection.FlushAsync();

        await stringCollection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);
        await stringCollection.WaitForIndexBuildAsync("float_vector");
        await stringCollection.LoadAsync();

        var queryParams = new QueryParameters
        {
            Offset = 3,
            Limit = 4,
            OutputFields = { "id", "text" }
        };

        var iterator = stringCollection.QueryWithIteratorAsync(parameters: queryParams);

        List<IReadOnlyList<FieldData>> results = new();
        await foreach (var result in iterator)
        {
            results.Add(result);
        }

        var allIds = results
            .SelectMany(r => ((FieldData<string>)r.First(f => f.FieldName == "id")).Data)
            .OrderBy(x => x)
            .ToList();

        Assert.True(allIds.Count <= 4);
        Assert.True(allIds.Count > 0);
    }

    [Fact]
    public async Task QueryWithIterator_with_offset_and_expression()
    {
        await Collection.InsertAsync(
        [
            FieldData.Create("id", Enumerable.Range(1, 20).Select(i => (long)i).ToArray()),
            FieldData.CreateVarChar("varchar", Enumerable.Range(1, 20).Select(i => $"text{i}").ToArray()),
            FieldData.CreateFloatVector("float_vector",
                Enumerable.Range(1, 20).Select(i => new ReadOnlyMemory<float>(new[] { i, 0f, 0f, 0f })).ToArray())
        ]);
        await Collection.FlushAsync();

        await Collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);
        await Collection.WaitForIndexBuildAsync("float_vector");
        await Collection.LoadAsync();

        var queryParams = new QueryParameters
        {
            Offset = 3,
            Limit = 5,
            OutputFields = { "id" }
        };

        var iterator = Collection.QueryWithIteratorAsync(expression: "id >= 5 and id <= 15", parameters: queryParams, batchSize: 2);

        List<IReadOnlyList<FieldData>> results = new();
        await foreach (var result in iterator)
        {
            results.Add(result);
        }

        var allIds = results
            .SelectMany(r => ((FieldData<long>)r.First(f => f.FieldName == "id")).Data)
            .OrderBy(x => x)
            .ToList();

        Assert.True(allIds.Count <= 5);
        Assert.True(allIds.Count > 0);
        Assert.All(allIds, id => Assert.True(id >= 5 && id <= 15));
    }

    [Fact]
    public async Task QueryWithIterator_large_offset()
    {
        await Collection.InsertAsync(
        [
            FieldData.Create("id", Enumerable.Range(1, 100).Select(i => (long)i).ToArray()),
            FieldData.CreateVarChar("varchar", Enumerable.Range(1, 100).Select(i => $"text{i}").ToArray()),
            FieldData.CreateFloatVector("float_vector",
                Enumerable.Range(1, 100).Select(i => new ReadOnlyMemory<float>(new[] { i, 0f, 0f, 0f })).ToArray())
        ]);
        await Collection.FlushAsync();

        await Collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);
        await Collection.WaitForIndexBuildAsync("float_vector");
        await Collection.LoadAsync();

        var queryParams = new QueryParameters
        {
            Offset = 90,
            Limit = 10,
            OutputFields = { "id" }
        };

        var iterator = Collection.QueryWithIteratorAsync(parameters: queryParams);

        List<IReadOnlyList<FieldData>> results = new();
        await foreach (var result in iterator)
        {
            results.Add(result);
        }

        var allIds = results
            .SelectMany(r => ((FieldData<long>)r.First(f => f.FieldName == "id")).Data)
            .ToList();

        Assert.True(allIds.Count <= 10);
        Assert.All(allIds, id => Assert.True(id > 90));
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

    private const string CollectionName = nameof(QueryWithIteratorExtendedTests);

    private readonly MilvusClient Client;

    private MilvusCollection Collection { get; }

    public QueryWithIteratorExtendedTests(MilvusFixture milvusFixture)
    {
        Client = milvusFixture.CreateClient();
        Collection = Client.GetCollection(CollectionName);
    }
}
