namespace Milvus.Client.V2.Types;

/// <summary>
/// The retry policy for RPC calls, aligned with the Java <c>RetryConfig</c> / C++ <c>RetryParam</c>.
/// </summary>
public sealed class RetryConfig
{
    /// <summary>
    /// The maximum number of retry attempts. A value of 1 disables retrying.
    /// </summary>
    public int MaxRetryTimes { get; set; } = 75;

    /// <summary>
    /// The initial backoff before the first retry.
    /// </summary>
    public TimeSpan InitialBackOff { get; set; } = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// The maximum backoff between retries.
    /// </summary>
    public TimeSpan MaxBackOff { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The multiplier applied to the backoff after each attempt (exponential backoff).
    /// </summary>
    public int BackOffMultiplier { get; set; } = 3;

    /// <summary>
    /// Whether to retry on <c>RateLimit</c> server errors. Defaults to <c>true</c>.
    /// </summary>
    public bool RetryOnRateLimit { get; set; } = true;

    /// <summary>
    /// An optional overall timeout for the whole retry loop. <c>null</c> means no overall cap (only
    /// <see cref="MaxRetryTimes" /> applies).
    /// </summary>
    public TimeSpan? MaxRetryTimeout { get; set; }
}
