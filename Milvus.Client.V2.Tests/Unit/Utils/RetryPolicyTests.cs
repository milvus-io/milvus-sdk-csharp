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

    [Fact]
    public async Task ExecuteAsync_extraRetryable_retries_and_notifies_per_attempt()
    {
        const string marker = "STREAMING_CODE_REPLICATE_VIOLATION";
        RetryConfig config = Config();
        int attempts = 0;
        int notified = 0;

        // A global-cluster routing error (MilvusException with the marker) is not retryable by default; the
        // extraRetryable predicate makes the retry loop treat it like the Java SDK does and fires
        // onRetryableAttempt before each backoff.
        int result = await RetryPolicy.ExecuteAsync<int>(
            _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new MilvusException(MilvusErrorCode.UnexpectedError, $"routing {marker}");
                }

                return Task.FromResult(42);
            },
            config,
            CancellationToken.None,
            extraRetryable: ex => ex.Message.Contains(marker, StringComparison.Ordinal),
            onRetryableAttempt: _ => notified++);

        Assert.Equal(42, result);
        Assert.Equal(3, attempts);
        Assert.Equal(2, notified); // one notification per retried attempt
    }

    [Fact]
    public async Task ExecuteAsync_without_extraRetryable_does_not_retry_plain_server_error()
    {
        RetryConfig config = Config();
        int attempts = 0;

        // Without the predicate, a non-rate-limit server error is not retried and no notification fires.
        await Assert.ThrowsAsync<MilvusException>(() =>
            RetryPolicy.ExecuteAsync<int>(
                _ =>
                {
                    attempts++;
                    throw new MilvusException(MilvusErrorCode.UnexpectedError, "server error");
                },
                config,
                CancellationToken.None,
                onRetryableAttempt: _ => throw new Xunit.Sdk.XunitException("must not be notified")));

        Assert.Equal(1, attempts);
    }
}
