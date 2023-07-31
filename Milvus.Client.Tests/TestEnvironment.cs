using Microsoft.Extensions.Configuration;

namespace Milvus.Client.Tests;

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
    {
        Host = Config["Host"] ?? "localhost";
        Username = Config["Username"] ?? "root";
        Password = Config["Password"] ?? "Milvus";

        Client = CreateClient();
    }

    public static string Host { get; private set; }
    public static string Username { get; private set; }
    public static string Password { get; private set; }

    public static MilvusClient CreateClient()
        => new MilvusClient(Host, username: Username, password: Password);

    public static MilvusClient Client { get; }
}
