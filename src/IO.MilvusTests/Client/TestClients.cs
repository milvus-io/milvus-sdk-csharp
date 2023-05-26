using IO.Milvus.Client;
using IO.Milvus.Client.gRPC;
using IO.Milvus.Client.REST;
using System;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace IO.MilvusTests.Client;

internal class TestClients : TheoryData<IMilvusClient2>
{
    public TestClients()
    {
        //var zillizClound = new MilvusGrpcClient("https://in01-5a0bcd24f238dca.aws-us-west-2.vectordb.zillizcloud.com", 19536, "db_admin", "Milvus-SDK-CSharp1");
        //Add(zillizClound);

        var restClient = new MilvusRestClient(HostConfig.Host, HostConfig.RestPort);
        Add(restClient);

        //var grpcClient = new MilvusGrpcClient(HostConfig.Host, HostConfig.Port);
        //Add(grpcClient);
    }
}
