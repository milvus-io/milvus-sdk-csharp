using IO.Milvus.Client;

namespace IO.MilvusTests.Utils;

internal static class BDDExtensions
{
    public static async Task ThenDropCollectionAsync(
        this IMilvusClient milvusClient,
        string collectionName)
    {
        if (await milvusClient.HasCollectionAsync(collectionName))
        {
            await milvusClient.DropCollectionAsync(collectionName);
        }
    }

    public static async Task GivenNoCollection(
        this IMilvusClient milvusClient,
        string collectionName)
    {
        if (await milvusClient.HasCollectionAsync(collectionName))
        {
            await milvusClient.DropCollectionAsync(collectionName);
        }
    }

    public static async Task GivenCollection(
        this IMilvusClient milvusClient,
        string collectionName)
    {
        await milvusClient.CreateBookCollectionAsync(collectionName);
    }

    public static async Task GivenPartitionAsync(
        this IMilvusClient milvusClient,
        string collectionName,
        string partitionName)
    {
        await milvusClient.CreatePartitionAsync(collectionName, partitionName);
    }

    public static async Task GivenLoadCollectionAsync(this IMilvusClient milvusClient, string collectionName) =>
        await milvusClient.LoadCollectionAsync(collectionName);

    public static async Task GivenBookIndex(
        this IMilvusClient milvusClient,
        string collectionName,
        string partitionName = "")
    {
        await milvusClient.CreateBookCollectionAndIndex(collectionName, partitionName);
    }
}