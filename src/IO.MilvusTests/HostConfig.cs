/*
 * Check https://github.com/microsoft/testfx for more information about tests.
 *
 */

using IO.Milvus.Param;

namespace IO.MilvusTests
{
    /// <summary>
    /// Milvus instance in my home,replace it with your config before run unittest.
    /// </summary>
    public sealed class HostConfig
    {
        public const string Host = "localhost";

        public const int Port = 19530;

        public const string DefaultTestCollectionName = "test";

        public const string DefaultTestPartitionName = "testPartitionName";

        public const string DefaultAliasName = "testAlias";

        public static ConnectParam ConnectParam
        {
            get
            {
                var connect = ConnectParam.Create(Host, Port);

                // In case we want to connect to a cloud service such as https://cloud.zilliz.com/
                connect.UseHttps = false;

                return connect;
            }
        }
    }
}
