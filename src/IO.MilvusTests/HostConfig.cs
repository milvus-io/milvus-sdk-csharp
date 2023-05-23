/*
 * Check https://github.com/microsoft/testfx for more information about tests.
 *
 */

using IO.Milvus.Param;

namespace IO.MilvusTests;

/// <summary>
/// Milvus instance in my home,replace it with your config before run unittest.
/// </summary>
public sealed class HostConfig
{
    public static string Host = "localhost";

    public static int Port = 19530;

    public static ConnectParam ConnectParam
    {
        get
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Milvus_Host")) == false)
            {
                Host = Environment.GetEnvironmentVariable("Milvus_Host")!;
                Port = int.Parse(Environment.GetEnvironmentVariable("Milvus_Port")!);
                var username = Environment.GetEnvironmentVariable("Milvus_Username");
                var password = Environment.GetEnvironmentVariable("Milvus_Password");
                var useHttps = bool.Parse(Environment.GetEnvironmentVariable("Milvus_UseHttps")!);

                var connect = ConnectParam.Create(Host, Port, username, password, useHttps);

                return connect;
            }
            else
            {
                var connect = ConnectParam.Create(Host, Port, "root", "milvus", false);

                return connect;
            }
        }
    }
}