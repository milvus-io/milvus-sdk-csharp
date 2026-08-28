using Testcontainers.Milvus;
using Xunit;

[assembly: AssemblyFixture(typeof(Milvus.Client.Tests.MilvusFixture))]

namespace Milvus.Client.Tests;

public sealed class MilvusFixture : IAsyncLifetime
{
    private const string DefaultMilvusImage = "milvusdb/milvus:v2.6.4";

    private readonly MilvusContainer _container =
        new MilvusBuilder(Environment.GetEnvironmentVariable("MILVUS_IMAGE") ?? DefaultMilvusImage)
            .WithEnvironment("QUOTA_AND_LIMITS_FLUSH_RATE_COLLECTION_MAX", "-1")
            .Build();

    public string Host => _container.Hostname;
    public int Port => _container.GetMappedPublicPort(MilvusBuilder.MilvusGrpcPort);
    public string Username => "root";
    public string Password => "Milvus";

    public MilvusClient CreateClient()
        => new(Host, Username, Password, Port, ssl: false);

    public MilvusClient CreateClient(string database)
        => new(Host, Username, Password, Port, ssl: false, database);

    public ValueTask InitializeAsync() => new(_container.StartAsync());
    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
