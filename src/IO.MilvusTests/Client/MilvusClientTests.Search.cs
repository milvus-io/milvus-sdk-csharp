using IO.Milvus.ApiSchema;
using IO.Milvus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IO.Milvus.Client;
using System.Runtime.InteropServices;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [TestMethod]
    [TestClientProvider]
    public async Task SearchTest(IMilvusClient2 milvusClient)
    {
        string collectionName = milvusClient.GetType().Name;

        bool collectionExist = await milvusClient.HasCollectionAsync(collectionName);

        if (collectionExist)
        {
            await milvusClient.DropCollectionAsync(collectionName);
        }

        Random ran = new Random();
        List<long> book_id_array = new ();
        List<long> word_count_array = new ();
        List<List<float>> book_intro_array = new ();
        for (long i = 0L; i < 2000; ++i)
        {
            book_id_array.Add(i);
            word_count_array.Add(i + 10000);
            List<float> vector = new ();
            for (int k = 0; k < 2; ++k)
            {
                vector.Add(ran.Next());
            }
            book_intro_array.Add(vector);
        }

        await milvusClient.CreateCollectionAsync(
            collectionName,
            new[] {
                new FieldType("book_id",MilvusDataType.Int64,true),
                new FieldType("word_count",MilvusDataType.Int64,false),
                FieldType.CreateFloatVector("book_intro",2),
            }
        );

        bool exist = await milvusClient.HasCollectionAsync(collectionName);
        Assert.IsTrue(exist, "Create collection failed");

        MilvusMutationResult result = await milvusClient.InsertAsync(collectionName,
            new[]
            {
                Field.Create("book_id",book_id_array),
                Field.Create("word_count",word_count_array),
                Field.CreateFloatVector("book_intro",book_intro_array),
            });

        Assert.IsTrue(result.InsertCount == 2000, "Insert data failed");
        Assert.IsTrue(result.SuccessIndex.Count == 2000, "Insert data failed");

        await milvusClient.CreateIndexAsync(collectionName, "book_id",new Dictionary<string,string> { { "nlist","1024"} });

        var indexBuildProgress = await milvusClient.GetIndexBuildProgress(collectionName, "book_id");

        await milvusClient.LoadCollectionAsync(collectionName);

        //Search
        List<String> search_output_fields = new (){ "book_id"};
        List<List<float>> search_vectors = new() { new() { 0.1f, 0.2f } };
        var searchResult = milvusClient.SearchAsync(
            MilvusSearchParameters.Create(collectionName)
            .WithVectors(search_vectors)
            .WithConsistencyLevel(MilvusConsistencyLevel.Strong)            
            .WithMetricType(MilvusMetricType.L2)
            .WithOutputFields(search_output_fields)
            .WithTopK(topK:2)
            .WithParameter("nprobe","10")
            .WithParameter("offset", "5")
            );

        Assert.IsNotNull(searchResult);
    }
}
