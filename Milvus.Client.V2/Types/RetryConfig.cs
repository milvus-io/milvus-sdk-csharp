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
    /// An optional overall timeout for the whole retry loop. <c>null</c> (or <c>TimeSpan.Zero</c>, mirroring the
    /// Java SDK's default of 0) means no overall cap; only <see cref="MaxRetryTimes" /> applies.
    /// </summary>
    public TimeSpan? MaxRetryTimeout { get; set; }

    // Validates the retry configuration, mirroring the Java SDK's RetryConfig builder rules
    // (non-negative backoffs, multiplier >= 1, at least one retry). Called from the client constructor.
    internal void Validate()
    {
        if (MaxRetryTimes < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxRetryTimes), MaxRetryTimes,
                "MaxRetryTimes must be at least 1.");
        }

        if (InitialBackOff < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(InitialBackOff), InitialBackOff,
                "InitialBackOff cannot be negative.");
        }

        if (MaxBackOff < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxBackOff), MaxBackOff,
                "MaxBackOff cannot be negative.");
        }

        if (BackOffMultiplier < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(BackOffMultiplier), BackOffMultiplier,
                "BackOffMultiplier must be at least 1.");
        }

        if (MaxRetryTimeout is { } timeout && timeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxRetryTimeout), timeout,
                "MaxRetryTimeout cannot be negative.");
        }
    }
}
