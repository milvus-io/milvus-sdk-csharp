using Xunit;

[assembly: AssemblyFixture(typeof(Milvus.Client.V2.Tests.MilvusV2Fixture))]

namespace Milvus.Client.V2.Tests;

/// <summary>
/// Assembly fixture that owns the Milvus test container for the whole test assembly.
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
