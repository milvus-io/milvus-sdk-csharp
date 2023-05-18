using IO.Milvus.Client;
using IO.Milvus.Client.gRPC;
using IO.Milvus.Client.REST;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Reflection;

namespace IO.MilvusTests.Client;

[AttributeUsage(AttributeTargets.Method)]
internal class TestClientProviderAttribute : Attribute, ITestDataSource
{
    public IEnumerable<object[]> GetData(MethodInfo methodInfo)
    {
        return new [] 
        {
            new IMilvusClient2[]{ new MilvusRestClient(HostConfig.Host, HostConfig.RestPort) },
            new IMilvusClient2[]{ new MilvusGrpcClient(HostConfig.Host, HostConfig.Port) },
        };
    }

    public string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        if (data != null)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data));
        }

        return null;
    }
}
