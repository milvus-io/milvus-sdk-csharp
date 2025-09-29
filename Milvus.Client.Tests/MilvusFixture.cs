using DotNet.Testcontainers.Builders;
using Testcontainers.Milvus;
using Xunit;

namespace Milvus.Client.Tests;

[CollectionDefinition("Milvus")]
public sealed class MilvusTestCollection : ICollectionFixture<MilvusFixture>;

public sealed class MilvusFixture : IAsyncLifetime
{
    private const string DefaultMilvusImage = "milvusdb/milvus:v2.6.2";

    private readonly MilvusContainer _container = new MilvusBuilder()
        .WithImage(Environment.GetEnvironmentVariable("MILVUS_IMAGE") ?? DefaultMilvusImage)
        .WithEnvironment("DEPLOY_MODE", "STANDALONE")
        .WithResourceMapping("./milvus.yaml", "/milvus/configs/")
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilMessageIsLogged(".*Proxy successfully started.*",
                o => o.WithTimeout(TimeSpan.FromMinutes(1))))
        .Build();

    public string Host => _container.Hostname;
    public int Port => _container.GetMappedPublicPort(MilvusBuilder.MilvusGrpcPort);
    public string Username => "root";
    public string Password => "Milvus";

    public MilvusClient CreateClient()
        => new(Host, Username, Password, Port, ssl: false);

    public MilvusClient CreateClient(string database)
        => new(Host, Username, Password, Port, ssl: false, database);

    public Task InitializeAsync() =>_container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
