namespace Milvus.Client.V2.Types;

/// <summary>
/// Configuration for the client-side telemetry manager, mirroring the Java/C++/Rust V2 SDKs' <c>TelemetryConfig</c>.
/// </summary>
public sealed class TelemetryConfig
{
    /// <summary>
    /// Whether telemetry is enabled. Defaults to <c>true</c>; when <c>false</c>, no heartbeat traffic is emitted.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The heartbeat interval in milliseconds. Defaults to 10000 (10 seconds); values ≤ 0 normalize to the
    /// default. The server may push a config that overrides this while the client is connected.
    /// </summary>
    public long HeartbeatIntervalMs { get; set; } = 10_000;

    /// <summary>
    /// The fraction of operations sampled for metrics, in [0, 1]. Defaults to 1.0 (sample everything).
    /// </summary>
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>
    /// The maximum number of recent telemetry errors retained. Defaults to 100; values ≤ 0 normalize to 100.
    /// </summary>
    public int ErrorMaxCount { get; set; } = 100;

    /// <summary>
    /// An optional stable client id. When empty, a random id is generated and reported as unstable.
    /// </summary>
    public string? ClientId { get; set; }

    internal TelemetryConfig Normalized()
    {
        var config = new TelemetryConfig
        {
            Enabled = Enabled,
            HeartbeatIntervalMs = HeartbeatIntervalMs > 0 ? HeartbeatIntervalMs : 10_000,
            SamplingRate = !double.IsNaN(SamplingRate) && !double.IsInfinity(SamplingRate)
                ? Math.Min(1, Math.Max(0, SamplingRate))
                : 0,
            ErrorMaxCount = ErrorMaxCount > 0 ? ErrorMaxCount : 100,
            ClientId = ClientId
        };
        return config;
    }
}
