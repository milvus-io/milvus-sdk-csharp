using Grpc.Core;
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

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await WaitForReadyAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private async Task WaitForReadyAsync()
    {
        using var client = CreateClient();

        var timeout = TimeSpan.FromSeconds(2);
        var start = DateTime.UtcNow;

        while (true)
        {
            try
            {
                await client.GetVersionAsync();
                return;
            }
            catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.Unknown)
            {
                if (DateTime.UtcNow - start > timeout)
                {
                    throw;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }
}
