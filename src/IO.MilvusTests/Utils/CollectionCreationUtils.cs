using IO.Milvus.ApiSchema;
using IO.Milvus.Client;
using IO.Milvus;
using Xunit;
using FluentAssertions;
using IO.Milvus.Client.REST;

namespace IO.MilvusTests.Utils;

internal static class CollectionCreationUtils
{
    internal static async Task CreateBookCollectionAndIndex(
        this IMilvusClient2 milvusClient, 
        string collectionName,
        string partitionName = "")
    {
        await CreateBookCollectionAsync(milvusClient, collectionName,partitionName);

        await InsertDataToBookCollection(milvusClient, collectionName, partitionName);

        await CreateIndexAsync(milvusClient, collectionName);
    }

    internal static async Task CreateBookCollectionAsync(
        this IMilvusClient2 milvusClient, 
        string collectionName, 
        string partitionName = "")
    {
        bool collectionExist = await milvusClient.HasCollectionAsync(collectionName);

        if (collectionExist)
        {
            await milvusClient.DropCollectionAsync(collectionName);
        }

        await milvusClient.CreateCollectionAsync(
            collectionName,
            new[] {
                FieldType.Create<long>("book_id",isPrimaryKey:true),
                FieldType.Create<long>("word_count"),
                FieldType.CreateVarchar("book_name",256),
                FieldType.CreateFloatVector("book_intro",2),
            }
        );

        bool exist = await milvusClient.HasCollectionAsync(collectionName);
        Assert.True(exist, "Create collection failed");

        if (!string.IsNullOrEmpty(partitionName))
        {
            await milvusClient.CreatePartitionAsync(collectionName, partitionName);

            bool result = await milvusClient.HasPartitionAsync(collectionName, partitionName);
            result.Should().BeTrue();
        }
    }

    internal static async Task WaitLoadedAsync(
    this IMilvusClient2 milvusClient,
    string collectionName)
    {
        await milvusClient.LoadCollectionAsync(collectionName);

        if (milvusClient is MilvusRestClient)
        {
            await Task.Delay(5000);
            return;
        }
        var progress = await milvusClient.GetLoadingProgressAsync(collectionName);

        int times = 0;
        while (progress < 100)
        {
            await Task.Delay(1000);
            progress = await milvusClient.GetLoadingProgressAsync(collectionName);
            if (times > 5)
            {
                Assert.Fail("Out of times");
            }
        }
    }

    #region Private ===============================================================
    private static async Task CreateIndexAsync(
        this IMilvusClient2 milvusClient, 
        string collectionName)
    {
        await milvusClient.CreateIndexAsync(
            collectionName,
            "book_intro",
            "default",
            milvusClient.IsZillizCloud() ? MilvusIndexType.AUTOINDEX : MilvusIndexType.IVF_FLAT,
            MilvusMetricType.L2,
            new Dictionary<string, string> { { "nlist", "1024" } });

        var indexes = await milvusClient.DescribeIndexAsync(collectionName, "book_intro");
        Assert.True(indexes.Count > 0, "Create index failed");
    }

    private static async Task<MilvusMutationResult> InsertDataToBookCollection(
        IMilvusClient2 milvusClient, 
        string collectionName,
        string partitionName = "")
    {
        Random ran = new Random();
        List<long> bookIds = new();
        List<long> wordCounts = new();
        List<List<float>> bookIntros = new();
        List<string> bookNames = new();
        for (long i = 0L; i < 2000; ++i)
        {
            bookIds.Add(i);
            wordCounts.Add(i + 10000);
            bookNames.Add($"Book Name {i}");

            List<float> vector = new();
            for (int k = 0; k < 2; ++k)
            {
                vector.Add(ran.Next());
            }
            bookIntros.Add(vector);
        }

        MilvusMutationResult result = await milvusClient.InsertAsync(collectionName,
            new Field[]
            {
                Field.Create("book_id",bookIds),
                Field.Create("word_count",wordCounts),
                Field.Create("book_name",bookNames),
                Field.CreateFloatVector("book_intro",bookIntros),
            },
            partitionName);

        Assert.True(result.InsertCount == 2000, "Insert data failed");
        Assert.True(result.SuccessIndex.Count == 2000, "Insert data failed");

        return result;
    }
    #endregion
}
