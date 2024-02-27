using System.Globalization;
using Microsoft.Extensions.Configuration;
using Milvus.Client.Tests.TestContainer;
using Xunit;

namespace Milvus.Client.Tests;

[CollectionDefinition("Milvus")]
public sealed class QdrantCollection : ICollectionFixture<MilvusFixture>;

public sealed class MilvusFixture : IAsyncLifetime
{
    private readonly MilvusContainer _container = new MilvusBuilder().Build();

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
