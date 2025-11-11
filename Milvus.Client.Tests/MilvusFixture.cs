using Testcontainers.Milvus;
using Xunit;

namespace Milvus.Client.Tests;

[CollectionDefinition("Milvus")]
public sealed class MilvusTestCollection : ICollectionFixture<MilvusFixture>;

public sealed class MilvusFixture : IAsyncLifetime
{
    private const string DefaultMilvusImage = "milvusdb/milvus:v2.6.4";

    private readonly MilvusContainer _container = new MilvusBuilder()
        .WithImage(Environment.GetEnvironmentVariable("MILVUS_IMAGE") ?? DefaultMilvusImage)
        .WithEnvironment("DEPLOY_MODE", "STANDALONE")
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

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
