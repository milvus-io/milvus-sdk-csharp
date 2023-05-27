using IO.Milvus.Client;

namespace IO.MilvusTests.Utils;

internal static class MilvusServerUtils
{
    public static bool IsZillizCloud(this IMilvusClient2 client)
    {
        return client.Address.Contains("vectordb.zillizcloud.com",StringComparison.OrdinalIgnoreCase);
    }
}
