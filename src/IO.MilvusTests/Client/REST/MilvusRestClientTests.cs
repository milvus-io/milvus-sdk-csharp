using IO.Milvus.Client.REST;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IO.MilvusTests;
using IO.Milvus.ApiSchema;

namespace IO.Milvus.Client.REST.Tests;

[TestClass()]
public class MilvusRestClientTests
{
    [TestMethod()]
    public IMilvusClient2 MilvusRestClientTest()
    {
        var client = new MilvusRestClient(HostConfig.Host, HostConfig.RestPort);

        Assert.IsNotNull(client);

        return client;
    }

    [TestMethod()]
    public async Task CreateCollectionAsyncTest()
    {
        IMilvusClient2 client = MilvusRestClientTest();

        await client.CreateCollectionAsync(
            "Test",
            ConsistencyLevel.Strong,
            new [] {
                new FieldType("book",Grpc.DataType.Int64,true) }
            );
    }
}