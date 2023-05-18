using Microsoft.VisualStudio.TestTools.UnitTesting;
using IO.MilvusTests;

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
}