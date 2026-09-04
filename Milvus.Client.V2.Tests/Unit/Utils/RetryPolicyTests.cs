using Xunit;

using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Unit.Utils;

[Trait("Category", "Unit")]
public class RetryPolicyTests
{
    private static RetryConfig Config()
        => new()
        {
            MaxRetryTimes = 5,
            InitialBackOff = TimeSpan.FromMilliseconds(100),
            MaxBackOff = TimeSpan.FromMilliseconds(1000),
            BackOffMultiplier = 3
        };

    [Fact]
    public void GetBackOff_caps_at_max_backoff()
    {
        RetryConfig config = Config();

        // 100, 300, 900, then capped at 1000 (2700 would exceed MaxBackOff).
        Assert.Equal(TimeSpan.FromMilliseconds(100), RetryPolicy.GetBackOff(config, 1));
        Assert.Equal(TimeSpan.FromMilliseconds(300), RetryPolicy.GetBackOff(config, 2));
        Assert.Equal(TimeSpan.FromMilliseconds(900), RetryPolicy.GetBackOff(config, 3));
        Assert.Equal(TimeSpan.FromMilliseconds(1000), RetryPolicy.GetBackOff(config, 4));
        Assert.Equal(TimeSpan.FromMilliseconds(1000), RetryPolicy.GetBackOff(config, 5));
    }

    [Fact]
    public void GetBackOff_does_not_overflow_large_attempts()
    {
        RetryConfig config = Config();

        // A huge attempt count must not overflow long.Ticks during the multiplication.
        TimeSpan delay = RetryPolicy.GetBackOff(config, 1000000);
        Assert.Equal(config.MaxBackOff, delay);
    }

    [Fact]
    public async Task ExecuteAsync_throws_when_max_retry_timeout_expires()
    {
        RetryConfig config = new()
        {
            MaxRetryTimes = 10,
            InitialBackOff = TimeSpan.FromMilliseconds(1),
            MaxBackOff = TimeSpan.FromMilliseconds(10),
            BackOffMultiplier = 2,
            MaxRetryTimeout = TimeSpan.FromMilliseconds(50)
        };

        // Always fail with a retryable (RateLimit) error; the retry window expires before MaxRetryTimes is hit.
        // Assert the deadline-specific error (UnexpectedError + "Retry window") so this test actually pins the
        // MaxRetryTimeout logic rather than passing via the retries-exhausted path.
        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            RetryPolicy.ExecuteAsync<int>(
                _ => throw new MilvusException(MilvusErrorCode.RateLimit, "rate limited"),
                config,
                CancellationToken.None));
        Assert.Equal(MilvusErrorCode.UnexpectedError, exception.ErrorCode);
        Assert.Contains("Retry window", exception.Message, StringComparison.Ordinal);
    }
}
