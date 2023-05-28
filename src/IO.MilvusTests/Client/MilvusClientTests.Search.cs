using IO.Milvus.ApiSchema;
using IO.Milvus;
using IO.Milvus.Client;
using Xunit;
using IO.MilvusTests.Utils;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task SearchTest(IMilvusClient milvusClient)
    {
        string collectionName = milvusClient.GetType().Name;
        await milvusClient.CreateBookCollectionAndIndex(collectionName);

        await milvusClient.WaitLoadedAsync(collectionName);

        //Search
        List<string> search_output_fields = new() { "book_id" };
        List<List<float>> search_vectors = new() { new() { 0.1f, 0.2f } };
        var searchResult = await milvusClient.SearchAsync(
            MilvusSearchParameters.Create(collectionName, "book_intro", search_output_fields)
            .WithVectors(search_vectors)
            .WithConsistencyLevel(MilvusConsistencyLevel.Strong)
            .WithMetricType(MilvusMetricType.IP)
            .WithTopK(topK: 2)
            .WithParameter("nprobe", "10")
            .WithParameter("offset", "5")
            );

        Assert.Equal(1, searchResult.Results.FieldsData.Count);

        //Drop collection
        await milvusClient.DropCollectionAsync(collectionName);
        await Task.Delay(1000);
    }
}
