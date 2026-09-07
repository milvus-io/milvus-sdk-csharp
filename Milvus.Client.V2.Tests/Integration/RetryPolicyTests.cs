using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class RetryPolicyTests
{
    private static ConnectConfig Config(MockMilvusServer server)
        => new()
        {
            Uri = server.Uri,
            ChannelOptions = server.ChannelOptions,
            Retry = new RetryConfig
            {
                MaxRetryTimes = 5,
                InitialBackOff = TimeSpan.FromMilliseconds(1),
                MaxBackOff = TimeSpan.FromMilliseconds(10),
                BackOffMultiplier = 2,
                MaxRetryTimeout = TimeSpan.FromSeconds(10)
            }
        };

    [Fact]
    public async Task RateLimit_failures_are_retried()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = new(Config(server));

        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // The next 3 operations fail with RateLimit, then succeed.
        server.Service.FailNextCalls = 3;

        await client.CreateCollectionAsync(
            new CreateCollectionReq
            {
                CollectionName = "coll",
                Schema = new CollectionSchema
                {
                    Fields =
                    {
                        new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                        FieldSchema.CreateFloatVector("embedding", dimension: 4)
                    }
                }
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("coll", server.Service.LastCreatedCollectionName);
        Assert.Equal(4, server.Service.TotalCalls);   // 3 failures + 1 success
    }

    [Fact]
    public async Task Non_retryable_server_error_fails_immediately()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = new(Config(server));

        await client.ConnectAsync(TestContext.Current.CancellationToken);

        server.Service.FailureStatus = new Milvus.Client.Grpc.Status
        {
            Code = (int)MilvusErrorCode.CollectionNotFound,
            Reason = "not found"
        };

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            client.HasCollectionAsync(new HasCollectionReq { CollectionName = "x" }, TestContext.Current.CancellationToken));

        Assert.Equal(MilvusErrorCode.CollectionNotFound, exception.ErrorCode);
        Assert.Equal(1, server.Service.TotalCalls);   // no retries for non-rate-limit server errors
    }

    [Fact]
    public async Task Gives_up_after_max_retries()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = new(Config(server));

        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // Always fail with RateLimit.
        server.Service.FailureStatus = new Milvus.Client.Grpc.Status
        {
            Code = (int)MilvusErrorCode.RateLimit,
            Reason = "rate limited"
        };

        await Assert.ThrowsAsync<MilvusException>(() =>
            client.HasCollectionAsync(new HasCollectionReq { CollectionName = "x" }, TestContext.Current.CancellationToken));

        Assert.Equal(6, server.Service.TotalCalls);   // 1 initial + 5 retries (MaxRetryTimes=5)
    }
}
