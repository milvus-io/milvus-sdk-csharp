using Grpc.Core;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Utils;

/// <summary>
/// The retry policy applied around RPC calls: decides whether a failure is retryable (two-layer error-code
/// decision, see the design doc §5.2.4) and executes the call with exponential backoff. Aligned with the
/// Java/C++/PyMilvus SDKs.
/// </summary>
internal static class RetryPolicy
{
    /// <summary>
    /// gRPC transport codes that are never retried (identical blacklist across Java/C++/PyMilvus).
    /// </summary>
    private static readonly StatusCode[] NonRetryableRpcCodes =
    {
        // Cancelled is caller-initiated (e.g. Grpc.Net.Client surfaces a cancelled RPC as RpcException(Cancelled)),
        // so it must not be retried.
        StatusCode.Cancelled,
        StatusCode.DeadlineExceeded,
        StatusCode.PermissionDenied,
        StatusCode.Unauthenticated,
        StatusCode.InvalidArgument,
        StatusCode.AlreadyExists,
        StatusCode.ResourceExhausted,
        StatusCode.Unimplemented
    };

    /// <summary>
    /// Whether the given failure should be retried:
    /// <list type="bullet">
    /// <item><see cref="RpcException" /> (transport): retry unless the gRPC code is blacklisted
    /// (PyMilvus/Java semantics — e.g. <c>Unavailable</c> is retried).</item>
    /// <item><see cref="MilvusException" /> (server): retry only on <c>RateLimit</c> when
    /// <see cref="RetryConfig.RetryOnRateLimit" /> is set.</item>
    /// </list>
    /// </summary>
    public static bool IsRetryable(Exception exception, RetryConfig config)
        => exception switch
        {
            RpcException { StatusCode: var code } rpc
                when Array.IndexOf(NonRetryableRpcCodes, code) < 0 => true,
            MilvusException { ErrorCode: MilvusErrorCode.RateLimit } => config.RetryOnRateLimit,
            MilvusException { ErrorCode: MilvusErrorCode.LegacyRateLimit } => config.RetryOnRateLimit,
            _ => false
        };

    /// <summary>
    /// Computes the exponential backoff delay for the given attempt, capped at <see cref="RetryConfig.MaxBackOff" />.
    /// The multiplication is capped before it can overflow <see cref="long" />.
    /// </summary>
    public static TimeSpan GetBackOff(RetryConfig config, int attempt)
    {
        TimeSpan delay = config.InitialBackOff;
        for (int i = 1; i < attempt; i++)
        {
            // Cap the current delay before multiplying so the product can never overflow long.Ticks.
            TimeSpan baseDelay = delay > config.MaxBackOff ? config.MaxBackOff : delay;
            delay = TimeSpan.FromTicks(baseDelay.Ticks * config.BackOffMultiplier);
        }

        return delay > config.MaxBackOff ? config.MaxBackOff : delay;
    }

    /// <summary>
    /// Executes <paramref name="call" />, retrying on retryable failures with exponential backoff, bounded by
    /// <see cref="RetryConfig.MaxRetryTimes" /> and, when set, <see cref="RetryConfig.MaxRetryTimeout" />.
    /// </summary>
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> call,
        RetryConfig config,
        CancellationToken cancellationToken)
    {
        if (config.MaxRetryTimes <= 1)
        {
            return await call(cancellationToken).ConfigureAwait(false);
        }

        // A zero timeout mirrors the Java SDK's default of 0 and means "no overall cap", same as null.
        DateTime? deadline = config.MaxRetryTimeout is { } timeout && timeout > TimeSpan.Zero
            ? DateTime.UtcNow + timeout
            : null;

        int attempt = 0;
        Exception? lastError = null;
        while (true)
        {
            // Re-check the deadline before issuing the next call: a backoff sleep that straddles the deadline
            // must not allow one more (possibly arbitrarily long) RPC past MaxRetryTimeout.
            if (deadline is not null && DateTime.UtcNow >= deadline.Value)
            {
                throw lastError is null
                    ? new TimeoutException($"Retry window of {config.MaxRetryTimeout} exceeded.")
                    : new MilvusException(MilvusErrorCode.UnexpectedError,
                        $"Retry window of {config.MaxRetryTimeout} exceeded; last failure: {lastError.Message}",
                        lastError);
            }

            try
            {
                return await call(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsRetryable(ex, config)
                                       && attempt < config.MaxRetryTimes
                                       && (deadline is null || DateTime.UtcNow < deadline))
            {
                lastError = ex;
                attempt++;
                TimeSpan delay = GetBackOff(config, attempt);

                // Do not sleep past the overall deadline.
                if (deadline is not null)
                {
                    TimeSpan remaining = deadline.Value - DateTime.UtcNow;
                    if (delay > remaining)
                    {
                        throw lastError is { } last
                            ? new MilvusException(MilvusErrorCode.UnexpectedError,
                                $"Retry window of {config.MaxRetryTimeout} exceeded; last failure: {last.Message}",
                                last)
                            : new TimeoutException($"Retry window of {config.MaxRetryTimeout} exceeded.");
                    }
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
