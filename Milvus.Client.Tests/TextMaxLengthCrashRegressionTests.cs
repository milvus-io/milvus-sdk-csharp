using Xunit;

namespace Milvus.Client.Tests;

// Regression test for the server crash documented on TextTests's TextTestsCollection: a collection
// with a Text field lacking max_length makes Milvus 2.6.4's streaming-node flusher panic and take the
// whole server process down, on a delayed WAL-recovery pass rather than synchronously. Tracked upstream
// at milvus-io/milvus#53291.
//
// FieldSchema.ToGrpc() now rejects that exact shape client-side (for any field, not just ones built via
// CreateText) -- see FieldSchema.CreateText's documentation -- so CreateCollectionAsync below never
// actually reaches the server with it any more. This test still runs against its own IsolatedMilvusFixture
// container rather than MilvusFixture's assembly-wide shared one: it's the one place deliberately
// constructing the bad shape, so if the client-side guard is ever weakened or removed, the resulting
// server crash takes down only this test class, not the other 280+ tests sharing the normal container.
public class TextMaxLengthCrashRegressionTests : IClassFixture<IsolatedMilvusFixture>, IDisposable
{
    private readonly MilvusClient? Client;

    public TextMaxLengthCrashRegressionTests(IsolatedMilvusFixture fixture)
        => Client = fixture.IsAvailable ? fixture.CreateClient() : null;

    public void Dispose() => Client?.Dispose();

    // Text (and this crash) is 2.6+, which is also the version IsolatedMilvusFixture requires before it starts a
    // container, so Client is only null when this test is skipped.
    [MilvusFact(MinimumVersion = "2.6")]
    public async Task CreateCollection_without_max_length_fails()
    {
        Assert.NotNull(Client);

        MilvusCollection collection = Client.GetCollection(nameof(CreateCollection_without_max_length_fails));
        await collection.DropAsync(TestContext.Current.CancellationToken);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            Client.CreateCollectionAsync(
                nameof(CreateCollection_without_max_length_fails),
                new[]
                {
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.Create("content", MilvusDataType.Text),
                    FieldSchema.CreateFloatVector("vec", 4),
                }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Contains("MaxLength", exception.Message, StringComparison.Ordinal);
    }
}
