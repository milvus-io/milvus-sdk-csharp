using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class SearchWithIteratorExtendedTests : IAsyncLifetime
{
    [Fact]
    public async Task SearchWithIterator_binary_vector()
    {
        await Collection.DropAsync();
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateBinaryVector("binary_vector", 8),
            });

        var binaryCollection = Client.GetCollection(CollectionName);

        await binaryCollection.InsertAsync(
        [
            FieldData.Create("id", new[] { 1L, 2L, 3L, 4L, 5L }),
            FieldData.CreateBinaryVectors("binary_vector", new ReadOnlyMemory<byte>[]
            {
                new byte[] { 0x01 },
                new byte[] { 0x02 },
                new byte[] { 0x03 },
                new byte[] { 0x04 },
                new byte[] { 0x05 }
            })
        ]);
        await binaryCollection.FlushAsync();

        await binaryCollection.CreateIndexAsync("binary_vector", IndexType.BinFlat, SimilarityMetricType.Jaccard);
        await binaryCollection.WaitForIndexBuildAsync("binary_vector");
        await binaryCollection.LoadAsync();

        var queryVector = new ReadOnlyMemory<byte>[] { new byte[] { 0x01 } };
        var iterator = binaryCollection.SearchWithIteratorAsync(
            "binary_vector",
            queryVector,
            SimilarityMetricType.Jaccard,
            limit: 5,
            batchSize: 2);

        List<SearchResults> results = new();
        await foreach (var result in iterator)
        {
            results.Add(result);
        }

        int totalResults = results.Sum(r => r.Ids.LongIds!.Count);
        Assert.Equal(5, totalResults);
    }

    public async Task InitializeAsync()
    {
        await Collection.DropAsync();
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 4),
            });
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    private const string CollectionName = nameof(SearchWithIteratorExtendedTests);

    private readonly MilvusClient Client;

    private MilvusCollection Collection { get; }

    public SearchWithIteratorExtendedTests(MilvusFixture milvusFixture)
    {
        Client = milvusFixture.CreateClient();
        Collection = Client.GetCollection(CollectionName);
    }
}
