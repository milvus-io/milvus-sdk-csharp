using System.Globalization;
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
        Port = Config["Port"] is string p ? int.Parse(p, CultureInfo.InvariantCulture) : 19530;
        Username = Config["Username"] ?? "root";
        Password = Config["Password"] ?? "Milvus";
        Database = Config["Database"];

        Client = CreateClient();
    }

    public static string Host { get; private set; }
    public static int Port { get; set; }
    public static string Username { get; private set; }
    public static string Password { get; private set; }
    public static string? Database { get; private set; }

    public static MilvusClient CreateClient()
        => new MilvusClient(Host, Port, Username, Password, Database);

    public static MilvusClient CreateClientForDatabase(string database)
        => new MilvusClient(Host, Port, Username, Password, database);

    public static MilvusClient Client { get; }
}
