using Xunit;

namespace Milvus.Client.V2.Tests;

/// <summary>
/// Defines the "Milvus" test collection. Only System tests (which need a real server) join this
/// collection; Unit and Integration tests run without a container.
/// </summary>
[CollectionDefinition(Name)]
public sealed class MilvusV2Collection : ICollectionFixture<MilvusV2Fixture>
{
    public const string Name = "Milvus";
}

/// <summary>
/// Collection fixture that owns the Milvus test container. Created once when the first System test in
/// the <c>"Milvus"</c> collection starts, and disposed after the last one finishes, so the container is
/// started and stopped exactly once per System run.
/// </summary>
public sealed class MilvusV2Fixture : IAsyncLifetime
{
    private MilvusTestContainer? _container;

    /// <summary>
    /// Starts the Milvus test container before any test runs.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        _container = await MilvusTestContainer.StartAsync();
    }

    /// <summary>
    /// Stops and removes the Milvus test container after all tests have run.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a new <see cref="MilvusClientV2" /> connected to the test container.
    /// </summary>
    public MilvusClientV2 CreateClient()
        => _container?.CreateClient()
            ?? throw new InvalidOperationException("The Milvus test container has not been started yet.");
}
