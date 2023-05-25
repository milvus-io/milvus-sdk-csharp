using IO.Milvus.Param;
using IO.MilvusTests.Client.Base;

namespace IO.MilvusTests.Helpers;

public static class BDDExtensions
{
    public static R<RpcStatus> ThenDropCollection(this MilvusServiceClientTestsBase baseTest, string collectionName)
    {
        return baseTest.MilvusClient.DropCollection(collectionName);
    }

    public static R<RpcStatus> GivenNoCollection(this MilvusServiceClientTestsBase baseTest, string collectionName)
    {
        return baseTest.MilvusClient.DropCollection(collectionName);
    }

    public static async Task<R<RpcStatus>> GivenCollection(this MilvusServiceClientTestsBase baseTest,
        string collectionName)
    {
        return await baseTest.CreateBookCollectionAsync(collectionName);
    }

    public static R<RpcStatus> GivenPartition(this MilvusServiceClientTestsBase baseTest, string collectionName,
        string partitionName)
    {
        return
            baseTest.CreatePartition(collectionName, partitionName);
    }

    public static Task GivenLoadCollectionAsync(this MilvusServiceClientTestsBase baseTest, string collectionName) =>
        baseTest.LoadCollectionAsync(collectionName);

    public static void GivenBookIndex(this MilvusServiceClientTestsBase baseTest, string collectionName)
    {
        baseTest.CreateBookCollectionIndex(collectionName, "book_intro", "book_intro_index");
    }
}