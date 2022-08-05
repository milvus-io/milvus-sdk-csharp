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
    public class HostConfig
    {
        public const string Host = "192.168.100.139";

        public const int Port = 19530;

        public const string DefaultTestCollectionName = "test";

        public const string DefaultTestPartitionName = "testPartitionName";

        public const string DefaultAliasName = "testAlias";

        public static ConnectParam ConnectParam { get; } 
            = ConnectParam.Create(Host, Port);
    }
}
