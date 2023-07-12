using IO.Milvus.Client;
using Microsoft.Extensions.Configuration;

namespace IO.MilvusTests;

public static class TestEnvironment
{
    public static IConfiguration Config { get; } = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("config.json", optional: true)
        .AddJsonFile("config.test.json", optional: true)
        .AddEnvironmentVariables()
        .Build()
        .GetSection("Test:Milvus");

    static TestEnvironment()
        => Client = CreateClient();

    public static MilvusClient CreateClient()
    {
        var port = 19530;

        if (Config["Port"] is string p && !int.TryParse(p, out port))
        {
            throw new Exception("Couldn't parse port: " + p);
        }

        return new MilvusClient(
            Config["Address"] ?? "http://localhost",
            port,
            Config["Username"] ?? "root",
            Config["password"] ?? "milvus");
    }

    public static MilvusClient Client { get; }

    public static bool IsZillizCloud
        => Client.Address.Contains("vectordb.zillizcloud.com", StringComparison.OrdinalIgnoreCase);
}
