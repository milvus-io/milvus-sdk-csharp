using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Google.Protobuf;

using Milvus.Client.V2.Types;

namespace Milvus.Client.V2;

/// <summary>
/// A snapshot of a client's telemetry metrics for one logical operation over a heartbeat window.
/// </summary>
public sealed class TelemetrySnapshot
{
    internal TelemetrySnapshot(
        string operation, DateTimeOffset windowStart, DateTimeOffset windowEnd,
        MetricsInfo global, IReadOnlyDictionary<string, MetricsInfo> collectionMetrics,
        IReadOnlyList<double> globalLatencySamplesMicros)
    {
        Operation = operation;
        WindowStart = windowStart;
        WindowEnd = windowEnd;
        Global = global;
        CollectionMetrics = collectionMetrics;
        GlobalLatencySamplesMicros = globalLatencySamplesMicros;
    }

    /// <summary>
    /// The logical operation, e.g. <c>"Search"</c>.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// The start of the window this snapshot covers.
    /// </summary>
    public DateTimeOffset WindowStart { get; }

    /// <summary>
    /// The end of the window this snapshot covers.
    /// </summary>
    public DateTimeOffset WindowEnd { get; }

    /// <summary>
    /// The operation's metrics aggregated across all collections.
    /// </summary>
    public MetricsInfo Global { get; }

    /// <summary>
    /// The operation's metrics per collection, keyed by collection name.
    /// </summary>
    public IReadOnlyDictionary<string, MetricsInfo> CollectionMetrics { get; }

    /// <summary>
    /// The raw global-bucket latency samples (microseconds) retained by this snapshot's ring, used to compute an
    /// exact aggregate p99 across windows (Java/C++/Rust merge the retained samples).
    /// </summary>
    public IReadOnlyList<double> GlobalLatencySamplesMicros { get; }
}

/// <summary>
/// A recent telemetry error observed while recording an operation or heartbeating.
/// </summary>
public sealed class TelemetryError
{
    internal TelemetryError(
        DateTimeOffset timestamp, string operation, string? collection, string errorMessage, string? requestId)
    {
        Timestamp = timestamp;
        Operation = operation;
        Collection = collection;
        ErrorMessage = errorMessage;
        RequestId = requestId;
    }

    /// <summary>
    /// When the error was observed.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// The logical operation that failed.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// The collection the failed operation targeted, when known.
    /// </summary>
    public string? Collection { get; }

    /// <summary>
    /// The error message.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// An optional client request id.
    /// </summary>
    public string? RequestId { get; }
}

/// <summary>
/// A command the server pushed to this client, delivered in a heartbeat response.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:PropertiesShouldNotReturnArrays",
    Justification = "Byte payloads are idiomatic as byte[] in this SDK.")]
public sealed class ClientTelemetryCommand
{
    internal ClientTelemetryCommand(
        string commandId, string commandType, byte[] payload, long createTime, bool persistent, string? targetScope)
    {
        CommandId = commandId;
        CommandType = commandType;
        Payload = payload;
        CreateTime = createTime;
        Persistent = persistent;
        TargetScope = targetScope;
    }

    /// <summary>
    /// The command id.
    /// </summary>
    public string CommandId { get; }

    /// <summary>
    /// The command type (e.g. <c>"push_config"</c>, <c>"show_errors"</c>).
    /// </summary>
    public string CommandType { get; }

    /// <summary>
    /// The command payload (typically a JSON document).
    /// </summary>
    public byte[] Payload { get; }

    /// <summary>
    /// The server creation timestamp of the command.
    /// </summary>
    public long CreateTime { get; }

    /// <summary>
    /// Whether the command is persistent (affects the config hash).
    /// </summary>
    public bool Persistent { get; }

    /// <summary>
    /// The target scope of the command.
    /// </summary>
    public string? TargetScope { get; }
}

/// <summary>
/// The reply to a <see cref="ClientTelemetryCommand" />, delivered on the next heartbeat.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:PropertiesShouldNotReturnArrays",
    Justification = "Byte payloads are idiomatic as byte[] in this SDK.")]
public sealed class ClientTelemetryCommandReply
{
    /// <summary>
    /// Initializes a command reply.
    /// </summary>
    public ClientTelemetryCommandReply(string commandId, bool success, string errorMessage = "", byte[]? payload = null)
    {
        CommandId = commandId;
        Success = success;
        ErrorMessage = errorMessage;
        Payload = payload;
    }

    /// <summary>
    /// The id of the command being replied to.
    /// </summary>
    public string CommandId { get; }

    /// <summary>
    /// Whether the command was handled successfully.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// The error message when <see cref="Success" /> is <c>false</c>.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// The reply payload (typically a JSON document).
    /// </summary>
    public byte[]? Payload { get; }
}

/// <summary>
/// The client-side telemetry manager: records per-operation metrics, reports them to the server over a heartbeat
/// loop, and handles server-pushed commands. Mirrors the Java <c>ClientTelemetryManager</c> / C++ <c>GetTelemetry</c> /
/// Rust <c>telemetry()</c>.
/// </summary>
public sealed class ClientTelemetryManager : IDisposable
{
    private const int SamplingScale = 1_000_000_000;
    private const int SampleRingSize = 1000;
    private const long MaxBackoffMs = 30 * 60 * 1000;
    private const int SnapshotHardCap = 4096;
    private const long SnapshotHistoryTtlMs = 60 * 60 * 1000;
    private const int MaxReplyBytes = 1024 * 1024;
    private const string TruncationSuffix = "...(truncated)";

    /// <summary>
    /// The logical operations recorded by the telemetry manager, matching the Java/C++/Rust SDKs.
    /// </summary>
    public static readonly IReadOnlyList<string> Operations = ["Insert", "Delete", "Upsert", "Search", "HybridSearch", "Query", "RunAnalyzer"];

    /// <summary>
    /// The payload keys applied by the <c>push_config</c> handler, mirroring the reference SDKs.
    /// </summary>
    private static readonly string[] PushConfigKeys = ["enabled", "heartbeat_interval_ms", "sampling_rate"];

    private readonly TelemetryConfig _config;
    private readonly string _clientId;
    private readonly bool _clientIdStable;
    private readonly Grpc.ClientTelemetryService.ClientTelemetryServiceClient? _grpcClient;
    private readonly Func<string> _username;
    private readonly Func<string> _database;
    private readonly Func<string> _sdkVersion;
    private readonly Func<string>? _address;
    private readonly Func<CancellationToken, CallOptions>? _createCallOptions;
    private readonly ConcurrentQueue<TelemetryError> _errors = new();
    private ConcurrentDictionary<string, Collector> _collectors = new();
    private readonly ConcurrentDictionary<string, Func<ClientTelemetryCommand, ClientTelemetryCommandReply>> _handlers = new();
    private readonly Dictionary<string, Func<ClientTelemetryCommand, ClientTelemetryCommandReply>> _defaultHandlers;

    private readonly List<KeyValuePair<DateTimeOffset, TelemetrySnapshot>> _snapshots = new();
    private readonly object _snapshotsLock = new();

    private readonly Dictionary<string, (long CreateTime, ClientTelemetryCommandReply Reply)> _executedReplies = new();
    private readonly object _executedRepliesLock = new();
    private volatile string _configHash = "";

    private readonly List<ClientTelemetryCommandReply> _pendingReplies = new();
    private readonly object _repliesLock = new();

    private volatile bool _ready;
    private volatile bool _unsupported;
    private int _unsupportedStreak;
    private long _lastCommandTimestamp;
    private string? _lastHeartbeatError;
    private bool _allCollectionsEnabled;
    private readonly HashSet<string> _enabledCollections = new(StringComparer.Ordinal);
    private readonly object _collectionsLock = new();

    // Runtime-mutable telemetry knobs written by the push_config command on the heartbeat thread and read on
    // caller threads; kept as volatile/atomic fields so concurrent reads never observe torn values (the config
    // object itself is never mutated after construction).
    private volatile bool _enabled;
    private long _samplingRateBits;
    private long _heartbeatIntervalMs;

    private long _samplingAccumulator;

    private readonly CancellationTokenSource _cts = new();
    private int _disposed;
    private Task? _heartbeatTask;

    internal ClientTelemetryManager(
        TelemetryConfig config,
        Grpc.ClientTelemetryService.ClientTelemetryServiceClient? grpcClient,
        Func<string> username,
        Func<string> database,
        Func<string> sdkVersion,
        Func<CancellationToken, CallOptions>? createCallOptions = null,
        Func<string>? address = null)
    {
        _config = config.Normalized();
        _clientId = string.IsNullOrWhiteSpace(_config.ClientId) ? Guid.NewGuid().ToString() : _config.ClientId!;
        _clientIdStable = !string.IsNullOrWhiteSpace(_config.ClientId);
        _grpcClient = grpcClient;
        _username = username;
        _database = database;
        _sdkVersion = sdkVersion;
        _createCallOptions = createCallOptions;
        _address = address;
        _defaultHandlers = new Dictionary<string, Func<ClientTelemetryCommand, ClientTelemetryCommandReply>>(StringComparer.Ordinal)
        {
            ["push_config"] = HandlePushConfig,
            ["collection_metrics"] = HandleCollectionMetrics,
            ["show_errors"] = HandleShowErrors,
            ["show_latency_history"] = HandleShowLatencyHistory,
            ["get_config"] = HandleGetConfig
        };

        _enabled = _config.Enabled;
        _samplingRateBits = BitConverter.DoubleToInt64Bits(_config.SamplingRate);
        _heartbeatIntervalMs = _config.HeartbeatIntervalMs;
    }

    private bool Enabled => _enabled;

    private double SamplingRate => BitConverter.Int64BitsToDouble(Interlocked.Read(ref _samplingRateBits));

    private long HeartbeatIntervalMs => Interlocked.Read(ref _heartbeatIntervalMs);

    /// <summary>
    /// The telemetry configuration in effect.
    /// </summary>
    public TelemetryConfig Config
        => new()
        {
            Enabled = Enabled,
            HeartbeatIntervalMs = HeartbeatIntervalMs,
            SamplingRate = SamplingRate,
            ErrorMaxCount = _config.ErrorMaxCount,
            ClientId = _clientId
        };

    /// <summary>
    /// The client id reported to the server.
    /// </summary>
    public string ClientId => _clientId;

    /// <summary>
    /// Whether the telemetry manager is ready (the heartbeat loop has started).
    /// </summary>
    public bool IsReady => _ready;

    /// <summary>
    /// Whether the server supports the telemetry heartbeat protocol. <c>false</c> once an
    /// <c>UNIMPLEMENTED</c> response is observed.
    /// </summary>
    public bool IsSupported => !_unsupported;

    /// <summary>
    /// The config hash reported to the server (a SHA-256 digest over the persistent commands), or
    /// <c>null</c> before any persistent command has been applied.
    /// </summary>
    public string? ConfigHash => string.IsNullOrEmpty(_configHash) ? null : _configHash;

    /// <summary>
    /// The error of the last heartbeat, or <c>null</c> when the last heartbeat succeeded.
    /// </summary>
    public string? LastHeartbeatError => _lastHeartbeatError;

    internal long LastCommandTimestamp => _lastCommandTimestamp;

    /// <summary>
    /// The most recent telemetry errors, newest first.
    /// </summary>
    /// <param name="maxCount">The maximum number of errors to return.</param>
    public IReadOnlyList<TelemetryError> RecentErrors(int maxCount = 100)
    {
        maxCount = maxCount < 0 ? 0 : maxCount;
        var list = _errors.Reverse().Take(maxCount).ToList();
        return list;
    }

    /// <summary>
    /// The retained metrics snapshots (one per heartbeat window), newest first, bounded to one hour of history.
    /// </summary>
    public IReadOnlyList<TelemetrySnapshot> MetricsSnapshots()
    {
        lock (_snapshotsLock)
        {
            return _snapshots
                .Select(pair => pair.Value)
                .Reverse()
                .Take(SnapshotHardCap)
                .ToArray();
        }
    }

    /// <summary>
    /// Registers a handler for a server-pushed command type. The handler receives the command and returns a
    /// reply that is delivered on the next heartbeat. A handler registered for a built-in type
    /// (<c>push_config</c>, <c>collection_metrics</c>, <c>show_errors</c>, <c>show_latency_history</c>,
    /// <c>get_config</c>) overrides the built-in behavior.
    /// </summary>
    /// <param name="commandType">The command type to handle.</param>
    /// <param name="handler">The handler, invoked on the heartbeat loop.</param>
    public void RegisterCommandHandler(string commandType, Func<ClientTelemetryCommand, ClientTelemetryCommandReply> handler)
    {
        if (string.IsNullOrWhiteSpace(commandType))
        {
            throw new ArgumentException("Command type must not be empty.", nameof(commandType));
        }

        _handlers[commandType] = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    internal void Start()
    {
        if (!Enabled || _grpcClient is null)
        {
            return;
        }

        if (_heartbeatTask is not null)
        {
            return;
        }

        _ready = true;
        _heartbeatTask = Task.Run(HeartbeatLoopAsync);
    }

    internal void RecordOperation(string operation, string? collection, double latencyMicros, bool success, string? errorMessage, string? requestId = null)
    {
        if (!Enabled || !ShouldSample())
        {
            return;
        }

        Collector collector = _collectors.GetOrAdd(operation, static _ => new Collector());
        string key;
        lock (_collectionsLock)
        {
            key = collection is not null && (_allCollectionsEnabled || _enabledCollections.Contains(collection))
                ? collection
                : "";
        }

        // Mirror the Java reference: every operation updates the global bucket, and a collection-backed
        // operation additionally updates its per-collection bucket.
        collector.Global.Record(latencyMicros, success);
        if (key.Length > 0)
        {
            MetricBucket collectionBucket = collector.Collections.GetOrAdd(key, static _ => new MetricBucket());
            collectionBucket.Record(latencyMicros, success);
        }

        if (!success && !string.IsNullOrEmpty(errorMessage))
        {
            _errors.Enqueue(new TelemetryError(DateTimeOffset.UtcNow, operation, collection, errorMessage!, requestId));
            while (_errors.Count > _config.ErrorMaxCount)
            {
                _errors.TryDequeue(out _);
            }
        }
    }

    private bool ShouldSample()
    {
        double rate = SamplingRate;

        if (rate >= 1.0)
        {
            return true;
        }

        if (rate <= 0.0)
        {
            return false;
        }

        long step = Math.Max(1, (long)(rate * SamplingScale));
        long next = Interlocked.Add(ref _samplingAccumulator, step);
        return next >= SamplingScale && (next % SamplingScale) < step;
    }

    private ClientTelemetryCommandReply HandlePushConfig(ClientTelemetryCommand command)
    {
        try
        {
            using var doc = JsonDocument.Parse(command.Payload);
            JsonElement root = doc.RootElement;

            bool? enabled = null;
            long? heartbeatIntervalMs = null;
            double? samplingRate = null;

            if (root.TryGetProperty("enabled", out JsonElement enabledElement)
                && enabledElement.ValueKind != JsonValueKind.Null)
            {
                if (enabledElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    throw new ArgumentException("enabled must be a boolean");
                }

                enabled = enabledElement.GetBoolean();
            }

            if (root.TryGetProperty("heartbeat_interval_ms", out JsonElement intervalElement)
                && intervalElement.ValueKind != JsonValueKind.Null)
            {
                if (!intervalElement.TryGetInt64(out long ms))
                {
                    throw new ArgumentException("heartbeat_interval_ms must be an integer");
                }

                if (ms <= 0)
                {
                    throw new ArgumentException("heartbeat_interval_ms must be positive");
                }

                heartbeatIntervalMs = ms;
            }

            if (root.TryGetProperty("sampling_rate", out JsonElement rateElement)
                && rateElement.ValueKind != JsonValueKind.Null)
            {
                if (!rateElement.TryGetDouble(out double sampling))
                {
                    throw new ArgumentException("sampling_rate must be a number");
                }

                samplingRate = sampling;
            }

            // ttl_seconds is validated as an integer but never applied, matching the reference SDKs.
            if (root.TryGetProperty("ttl_seconds", out JsonElement ttlSeconds)
                && ttlSeconds.ValueKind != JsonValueKind.Null
                && !ttlSeconds.TryGetInt64(out _))
            {
                throw new ArgumentException("ttl_seconds must be an integer");
            }

            // No mutation occurs before every supplied value is known to be valid.
            if (enabled is { } enabledValue)
            {
                _enabled = enabledValue;
            }

            if (heartbeatIntervalMs is { } intervalValue)
            {
                Interlocked.Exchange(ref _heartbeatIntervalMs, intervalValue);
            }

            if (samplingRate is { } rateValue)
            {
                double normalized = !double.IsNaN(rateValue) && !double.IsInfinity(rateValue)
                    ? Math.Min(1, Math.Max(0, rateValue))
                    : 0;
                Interlocked.Exchange(ref _samplingRateBits, BitConverter.DoubleToInt64Bits(normalized));
            }

            // applied: the recognized settings that are present and non-null.
            var applied = new List<string>();
            foreach (string key in PushConfigKeys)
            {
                if (root.TryGetProperty(key, out JsonElement element) && element.ValueKind != JsonValueKind.Null)
                {
                    applied.Add(key);
                }
            }

            // ignored: every other payload key (including ttl_seconds), sorted, mirroring the reference.
            var ignored = new List<string>();
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!PushConfigKeys.Contains(property.Name))
                {
                    ignored.Add(property.Name);
                }
            }

            ignored.Sort(StringComparer.Ordinal);

            var result = new Dictionary<string, object?>
            {
                ["applied"] = applied
            };

            if (ignored.Count > 0)
            {
                result["ignored"] = ignored;
            }

            return Ok(command.CommandId, result);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            return new ClientTelemetryCommandReply(command.CommandId, false, ex.Message);
        }
    }

    private ClientTelemetryCommandReply HandleCollectionMetrics(ClientTelemetryCommand command)
    {
        if (command.Payload.Length == 0)
        {
            lock (_collectionsLock)
            {
                return Ok(command.CommandId, new
                {
                    all_collections_enabled = _allCollectionsEnabled,
                    enabled_collections = _enabledCollections.OrderBy(x => x, StringComparer.Ordinal).ToArray()
                });
            }
        }

        try
        {
            using var doc = JsonDocument.Parse(command.Payload);
            JsonElement root = doc.RootElement;
            bool enabled = root.TryGetProperty("enabled", out JsonElement e)
                && (e.ValueKind == JsonValueKind.True || e.ValueKind == JsonValueKind.False)
                ? e.GetBoolean()
                : false;
            string[] collections = root.TryGetProperty("collections", out JsonElement c) && c.ValueKind == JsonValueKind.Array
                ? c.EnumerateArray().Select(x => x.GetString()).Where(x => x is not null).Cast<string>().ToArray()
                : Array.Empty<string>();

            lock (_collectionsLock)
            {
                if (collections.Contains("*", StringComparer.Ordinal))
                {
                    // "*" toggles collection metrics globally.
                    _allCollectionsEnabled = enabled;
                    _enabledCollections.Clear();
                }
                else if (enabled)
                {
                    // Enabling a specific list: an empty list is invalid (would disable everything).
                    if (collections.Length == 0)
                    {
                        throw new ArgumentException("collection_metrics with enabled=true requires a non-empty collections list.");
                    }

                    // Merge the listed collections into the enabled set, leaving all_collections_enabled unchanged (Java addAll).
                    foreach (string collection in collections)
                    {
                        _enabledCollections.Add(collection);
                    }
                }
                else if (collections.Length == 0)
                {
                    // enabled=false with an empty list disables collection metrics entirely (Java: disable all).
                    _allCollectionsEnabled = false;
                    _enabledCollections.Clear();
                }
                else
                {
                    // Disabling removes the listed collections from the enabled set (removeAll).
                    foreach (string collection in collections)
                    {
                        _enabledCollections.Remove(collection);
                    }
                }
            }

            return Ok(command.CommandId, new { applied = true });
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            return new ClientTelemetryCommandReply(command.CommandId, false, ex.Message);
        }
    }

    private ClientTelemetryCommandReply HandleShowErrors(ClientTelemetryCommand command)
    {
        int maxCount = 100;
        try
        {
            using var doc = JsonDocument.Parse(command.Payload);
            if (doc.RootElement.TryGetProperty("max_count", out JsonElement max) && max.TryGetInt32(out int n))
            {
                maxCount = n <= 0 ? 100 : n;
            }
        }
        catch (JsonException)
        {
            // Default to the reference maximum.
        }

        var errors = RecentErrors(maxCount)
            .Select(error => new Dictionary<string, object?>
            {
                ["timestamp"] = error.Timestamp.ToUnixTimeMilliseconds(),
                ["operation"] = error.Operation,
                ["error_msg"] = error.ErrorMessage,
                ["collection"] = error.Collection,
                ["request_id"] = error.RequestId
            })
            .ToList();

        if (errors.Count == 0)
        {
            return new ClientTelemetryCommandReply(command.CommandId, true);
        }

        // Mirror the Java reference's 1MB reply cap: halve the list, then truncate the single remaining
        // error's longest truncatable field until the payload fits.
        byte[] encoded = JsonSerializer.SerializeToUtf8Bytes(errors);
        while (encoded.Length > MaxReplyBytes && errors.Count > 1)
        {
            errors.RemoveRange(errors.Count / 2, errors.Count - errors.Count / 2);
            encoded = JsonSerializer.SerializeToUtf8Bytes(errors);
        }

        while (encoded.Length > MaxReplyBytes && errors.Count == 1)
        {
            int previousSize = encoded.Length;
            TruncateLongestErrorField(errors[0]);
            encoded = JsonSerializer.SerializeToUtf8Bytes(errors);
            if (encoded.Length >= previousSize)
            {
                // Guarantee monotonic progress even when JSON escaping defeats the truncation.
                foreach (string field in TruncatableErrorFields)
                {
                    if (errors[0][field] is string { Length: > 0 } value)
                    {
                        errors[0][field] = "";
                        break;
                    }
                }

                encoded = JsonSerializer.SerializeToUtf8Bytes(errors);
                if (encoded.Length >= previousSize)
                {
                    break;
                }
            }
        }

        if (encoded.Length > MaxReplyBytes)
        {
            return new ClientTelemetryCommandReply(command.CommandId, false, "show_errors response exceeds the 1MB payload limit");
        }

        return new ClientTelemetryCommandReply(command.CommandId, true, "", encoded);
    }

    private static readonly string[] TruncatableErrorFields = ["error_msg", "collection", "operation", "request_id"];

    private static void TruncateLongestErrorField(Dictionary<string, object?> error)
    {
        string? longestField = null;
        string? longestValue = null;
        foreach (string candidate in TruncatableErrorFields)
        {
            if (error[candidate] is string value && (longestValue is null || value.Length > longestValue.Length))
            {
                longestField = candidate;
                longestValue = value;
            }
        }

        if (longestField is null || longestValue is not { Length: > 0 })
        {
            return;
        }

        string fieldName = longestField;
        if (longestValue.Length <= TruncationSuffix.Length + 1)
        {
            error[fieldName] = "";
            return;
        }

        int keep = longestValue.Length / 2;
        if (keep > TruncationSuffix.Length)
        {
            keep -= TruncationSuffix.Length;
        }
        else
        {
            keep = 0;
        }

        // Never split a UTF-16 surrogate pair so the encoded payload stays valid UTF-8.
        if (keep > 0 && char.IsHighSurrogate(longestValue[keep - 1]))
        {
            keep--;
        }

#pragma warning disable CA1845 // The SDK multi-targets netstandard2.0/net462 where span-based Concat is unavailable.
        error[fieldName] = keep > 0 ? string.Concat(longestValue.Substring(0, keep), TruncationSuffix) : "";
#pragma warning restore CA1845
    }

    private ClientTelemetryCommandReply HandleShowLatencyHistory(ClientTelemetryCommand command)
    {
        try
        {
            if (command.Payload.Length == 0)
            {
                return new ClientTelemetryCommandReply(command.CommandId, false, "payload is required with start_time and end_time");
            }

            using var doc = JsonDocument.Parse(command.Payload);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("start_time", out JsonElement startEl) && startEl.GetString() is { } startStr
                && root.TryGetProperty("end_time", out JsonElement endEl) && endEl.GetString() is { } endStr)
            {
                long start = DateTimeOffset.Parse(startStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUnixTimeMilliseconds();
                long end = DateTimeOffset.Parse(endStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUnixTimeMilliseconds();

                if (end < start)
                {
                    return new ClientTelemetryCommandReply(command.CommandId, false, "end_time must be after start_time");
                }

                if (end - start > SnapshotHistoryTtlMs)
                {
                    return new ClientTelemetryCommandReply(command.CommandId, false, "time range cannot exceed 1 hour");
                }

                bool detail = root.TryGetProperty("detail", out JsonElement detailEl) && detailEl.ValueKind is JsonValueKind.True or JsonValueKind.False
                    ? detailEl.GetBoolean()
                    : false;

                IReadOnlyList<TelemetrySnapshot> inRange = MetricsSnapshots()
                    .Where(snapshot => snapshot.WindowEnd.ToUnixTimeMilliseconds() >= start && snapshot.WindowStart.ToUnixTimeMilliseconds() <= end)
                    .ToList();

                object response = detail
                    ? DetailHistory(inRange)
                    : AggregateHistory(inRange, start, end);

                byte[] encoded = JsonSerializer.SerializeToUtf8Bytes(response);
                if (encoded.Length > MaxReplyBytes)
                {
                    return new ClientTelemetryCommandReply(command.CommandId, false, "response too large, try a smaller time range");
                }

                return new ClientTelemetryCommandReply(command.CommandId, true, "", encoded);
            }

            return new ClientTelemetryCommandReply(command.CommandId, false, "payload is required with start_time and end_time");
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            return new ClientTelemetryCommandReply(command.CommandId, false, "invalid payload");
        }
    }

    private static Dictionary<string, object?> DetailHistory(IReadOnlyList<TelemetrySnapshot> snapshots)
    {
        var details = new List<Dictionary<string, object?>>();
        foreach (IGrouping<DateTimeOffset, TelemetrySnapshot> group in snapshots.GroupBy(snapshot => snapshot.WindowEnd).OrderBy(group => group.Key))
        {
            var metrics = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (TelemetrySnapshot snapshot in group)
            {
                metrics[snapshot.Operation] = MetricHistory(snapshot.Global);
            }

            details.Add(new Dictionary<string, object?>
            {
                ["timestamp"] = group.First().WindowStart.ToUnixTimeMilliseconds(),
                ["end_time"] = group.Key.ToUnixTimeMilliseconds(),
                ["metrics"] = metrics
            });
        }

        return new Dictionary<string, object?>
        {
            ["snapshots"] = details,
            ["total_snapshots"] = details.Count
        };
    }

    private static Dictionary<string, object?> AggregateHistory(IReadOnlyList<TelemetrySnapshot> snapshots, long start, long end)
    {
        var metrics = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (IGrouping<string, TelemetrySnapshot> group in snapshots.GroupBy(snapshot => snapshot.Operation))
        {
            long requests = group.Sum(snapshot => snapshot.Global.RequestCount);
            long successes = group.Sum(snapshot => snapshot.Global.SuccessCount);
            long failures = group.Sum(snapshot => snapshot.Global.ErrorCount);
            double weightedLatencyMs = group.Sum(snapshot => snapshot.Global.AvgLatencyMs * snapshot.Global.RequestCount);
            double maxLatencyMs = group.Max(snapshot => snapshot.Global.MaxLatencyMs);

            // Merge each window's retained latency samples, weighted by requestCount/sampleCount, and compute an
            // exact aggregate p99 (Java WeightedSampleSet / C++ latency_windows / Rust latency_samples merge the
            // raw samples rather than combining per-window p99s).
            double p99LatencyMs = ExactWeightedP99(
                group.SelectMany(snapshot => snapshot.GlobalLatencySamplesMicros.Count == 0
                    ? Array.Empty<(double LatencyMs, double Weight)>()
                    : snapshot.GlobalLatencySamplesMicros.Select(sample => (
                        sample / 1000.0,
                        (double)snapshot.Global.RequestCount / snapshot.GlobalLatencySamplesMicros.Count))));

            metrics[group.Key] = new Dictionary<string, object?>
            {
                ["request_count"] = requests,
                ["success_count"] = successes,
                ["error_count"] = failures,
                ["avg_latency_ms"] = requests == 0 ? 0.0 : weightedLatencyMs / requests,
                ["p99_latency_ms"] = p99LatencyMs,
                ["max_latency_ms"] = maxLatencyMs
            };
        }

        return new Dictionary<string, object?>
        {
            ["aggregated"] = new Dictionary<string, object?>
            {
                ["start_time"] = start,
                ["end_time"] = end,
                ["metrics"] = metrics
            },
            ["snapshot_count"] = snapshots.Count
        };
    }

    private static Dictionary<string, object?> MetricHistory(MetricsInfo value)
        => new()
        {
            ["request_count"] = value.RequestCount,
            ["success_count"] = value.SuccessCount,
            ["error_count"] = value.ErrorCount,
            ["avg_latency_ms"] = value.AvgLatencyMs,
            ["p99_latency_ms"] = value.P99LatencyMs,
            ["max_latency_ms"] = value.MaxLatencyMs
        };

    private static double ExactWeightedP99(IEnumerable<(double LatencyMs, double Weight)> samples)
    {
        var sorted = samples.Where(sample => sample.Weight > 0).OrderBy(sample => sample.LatencyMs).ToList();
        if (sorted.Count == 0)
        {
            return 0.0;
        }

        double totalWeight = sorted.Sum(sample => sample.Weight);
        double threshold = totalWeight * 0.99;
        double cumulative = 0.0;
        foreach ((double latencyMs, double weight) in sorted)
        {
            cumulative += weight;
            if (cumulative >= threshold)
            {
                return latencyMs;
            }
        }

        return sorted[sorted.Count - 1].LatencyMs;
    }

    private ClientTelemetryCommandReply HandleGetConfig(ClientTelemetryCommand command)
    {
        lock (_collectionsLock)
        {
            var userConfig = new Dictionary<string, object?>
            {
                ["address"] = _address?.Invoke() ?? "",
                ["username"] = _username(),
                ["db_name"] = _database(),
                ["telemetry_enabled"] = Enabled,
                ["telemetry_heartbeat_interval_ms"] = HeartbeatIntervalMs,
                ["telemetry_sampling_rate"] = SamplingRate,
                ["enabled_collections"] = _allCollectionsEnabled
                    ? new[] { "*" }
                    : _enabledCollections.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                ["all_collections_enabled"] = _allCollectionsEnabled
            };

            return Ok(command.CommandId, new { user_config = userConfig });
        }
    }

    private static ClientTelemetryCommandReply Ok(string commandId, object value)
        => new(commandId, true, "", JsonSerializer.SerializeToUtf8Bytes(value));

    /// <summary>
    /// Stops the heartbeat loop and releases the manager's resources.
    /// </summary>
    public void Dispose()
    {
        // Idempotent dispose: the manager is publicly returned by GetTelemetry() and the owning client also
        // disposes it, so a double dispose must be a no-op (Java close() uses a compareAndSet guard).
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _cts.Cancel();
        try
        {
            _heartbeatTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Best-effort stop.
        }
        catch (OperationCanceledException)
        {
            // Best-effort stop.
        }

        _cts.Dispose();
    }

    private async Task HeartbeatLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            // Guard the whole iteration so an unexpected failure in snapshotting or request construction
            // records the error and the loop keeps running instead of dying permanently.
            try
            {
                List<KeyValuePair<DateTimeOffset, TelemetrySnapshot>> snapshot = CreateSnapshot();
                await SendHeartbeatAsync(snapshot).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Telemetry must never throw into the caller; the error is recorded and retried.
            catch (Exception ex)
            {
                _lastHeartbeatError = ex.Message;
            }
#pragma warning restore CA1031

            long interval = HeartbeatIntervalMs;
            if (_unsupportedStreak > 0)
            {
                // Saturate the backoff multiplier like the Java reference (nextDelayMs) so an extreme pushed
                // interval cannot overflow long or TimeSpan.FromMilliseconds and fault the heartbeat loop.
                int exponent = Math.Min(_unsupportedStreak, 30);
                long multiplier = 1L << Math.Min(exponent, 20);
                long delay = interval > MaxBackoffMs / multiplier ? MaxBackoffMs : interval * multiplier;
                interval = Math.Max(interval, Math.Min(MaxBackoffMs, delay));
            }

            interval = Math.Min(interval, (long)(TimeSpan.MaxValue.TotalMilliseconds - 1));

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(interval), _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    internal async Task SendHeartbeatAsync(List<KeyValuePair<DateTimeOffset, TelemetrySnapshot>> snapshot)
    {
        if (_grpcClient is null)
        {
            _lastHeartbeatError = "telemetry transport not attached";
            return;
        }

        var request = new Grpc.ClientHeartbeatRequest
        {
            ClientInfo = BuildClientInfo(),
            ReportTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ConfigHash = _configHash,
            LastCommandTimestamp = _lastCommandTimestamp
        };

        if (Enabled && snapshot.Count > 0)
        {
            // Send every operation's metrics in the latest window, like the Java reference
            // (addAllMetrics over the latest window).
            foreach (KeyValuePair<DateTimeOffset, TelemetrySnapshot> pair in snapshot)
            {
                if (pair.Value is not null)
                {
                    request.Metrics.Add(ToGrpcOperationMetrics(pair.Value));
                }
            }
        }

        List<ClientTelemetryCommandReply> sentReplies;
        lock (_repliesLock)
        {
            sentReplies = _pendingReplies.ToList();
        }

        foreach (ClientTelemetryCommandReply reply in sentReplies)
        {
            request.CommandReplies.Add(ToGrpcCommandReply(reply));
        }

        try
        {
            CallOptions options = (_createCallOptions?.Invoke(_cts.Token) ?? default).WithDeadline(DateTime.UtcNow.AddSeconds(10));
            using var call = _grpcClient.ClientHeartbeatAsync(request, options);
            Grpc.ClientHeartbeatResponse response = await call.ResponseAsync.ConfigureAwait(false);

            var code = (MilvusErrorCode)response.Status.Code;

            _unsupported = false;
            _unsupportedStreak = 0;
            _lastHeartbeatError = null;

            if (code != MilvusErrorCode.Success)
            {
                _lastHeartbeatError = response.Status.Reason;
                return;
            }

            // Only the replies that were actually sent are cleared on success.
            lock (_repliesLock)
            {
                if (sentReplies.Count > 0)
                {
                    _pendingReplies.RemoveRange(0, Math.Min(sentReplies.Count, _pendingReplies.Count));
                }
            }

            ProcessCommands(response.Commands);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unimplemented)
        {
            _unsupportedStreak++;
            _unsupported = true;
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
#pragma warning disable CA1031 // Telemetry must never throw into the caller; the error is recorded and retried.
        catch (Exception ex)
        {
            _lastHeartbeatError = ex.Message;
        }
#pragma warning restore CA1031
    }

    private Grpc.ClientInfo BuildClientInfo()
    {
        var info = new Grpc.ClientInfo
        {
            SdkType = "CSharp",
            SdkVersion = _sdkVersion(),
            LocalTime = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            User = _username(),
            Host = Environment.MachineName
        };
        info.Reserved["client_id"] = _clientId;
        info.Reserved["client_id_stable"] = _clientIdStable ? "true" : "false";
        string database = _database();
        if (!string.IsNullOrEmpty(database))
        {
            info.Reserved["db_name"] = database;
        }

        return info;
    }

    internal void ProcessCommands(Google.Protobuf.Collections.RepeatedField<Grpc.ClientCommand> commands)
    {
        long previousTimestamp = _lastCommandTimestamp;
        long maxTimestamp = previousTimestamp;
        bool hasPersistent = false;
        var persistentCommands = new List<ClientTelemetryCommand>();

        foreach (Grpc.ClientCommand command in commands)
        {
            maxTimestamp = Math.Max(maxTimestamp, command.CreateTime);
            hasPersistent |= command.Persistent;
            if (command.Persistent)
            {
                persistentCommands.Add(new ClientTelemetryCommand(
                    command.CommandId, command.CommandType, command.Payload.ToByteArray(),
                    command.CreateTime, command.Persistent, command.TargetScope));
            }

            if (command.CreateTime < previousTimestamp)
            {
                // ACK commands older than the cursor with an empty success reply (Java/C++/Rust do the same)
                // so a server-side command whose reply delivery failed is resolved instead of pending forever.
                lock (_repliesLock)
                {
                    _pendingReplies.Add(new ClientTelemetryCommandReply(command.CommandId, true));
                }

                continue;
            }

            // Re-delivered command: re-queue the stored reply so a redelivery cannot turn an already-reported
            // failure into a contradictory success (mirroring the Java reference).
            ClientTelemetryCommandReply? stored;
            lock (_executedRepliesLock)
            {
                stored = _executedReplies.TryGetValue(command.CommandId, out var entry) ? entry.Reply : null;
            }

            if (stored is not null)
            {
                lock (_repliesLock)
                {
                    _pendingReplies.Add(stored);
                }

                continue;
            }

            var clientCommand = new ClientTelemetryCommand(
                command.CommandId, command.CommandType, command.Payload.ToByteArray(),
                command.CreateTime, command.Persistent, command.TargetScope);

            ClientTelemetryCommandReply reply;
            if (_handlers.TryGetValue(command.CommandType, out var handler) || _defaultHandlers.TryGetValue(command.CommandType, out handler))
            {
#pragma warning disable CA1031 // A failing handler must not break the heartbeat loop; it is replied as a failure.
                try
                {
                    reply = handler(clientCommand);
                }
                catch (Exception ex)
                {
                    reply = new ClientTelemetryCommandReply(command.CommandId, false, ex.Message);
                }
#pragma warning restore CA1031
            }
            else
            {
                reply = new ClientTelemetryCommandReply(command.CommandId, false, $"Unknown command type: {command.CommandType}");
            }

            if (command.CommandId != reply.CommandId)
            {
                reply = new ClientTelemetryCommandReply(command.CommandId, reply.Success, reply.ErrorMessage, reply.Payload);
            }

            lock (_repliesLock)
            {
                _pendingReplies.Add(reply);
            }

            lock (_executedRepliesLock)
            {
                _executedReplies[command.CommandId] = (command.CreateTime, reply);
            }
        }

        // The server re-sends the current config whenever our hash is stale; recompute the hash over the
        // received (authoritative) persistent subset each time one is present, so deleted server-side configs
        // converge instead of accumulating forever.
        if (hasPersistent)
        {
            _configHash = CalculateConfigHash(persistentCommands);
        }

        _lastCommandTimestamp = Math.Max(_lastCommandTimestamp, maxTimestamp);

        // Retain only the replies for commands at or after the new cursor (the server may redeliver the batch).
        lock (_executedRepliesLock)
        {
            foreach (KeyValuePair<string, (long CreateTime, ClientTelemetryCommandReply Reply)> entry in _executedReplies
                .Where(entry => entry.Value.CreateTime < maxTimestamp).ToList())
            {
                _executedReplies.Remove(entry.Key);
            }
        }
    }

#pragma warning disable CA1850 // SHA256.HashData is not available on the netstandard2.0/net462 targets.
    private static string CalculateConfigHash(List<ClientTelemetryCommand> persistentCommands)
    {
        if (persistentCommands.Count == 0)
        {
            return "";
        }

        IEnumerable<ClientTelemetryCommand> ordered = persistentCommands
            .OrderBy(c => c.CommandId, StringComparer.Ordinal);
        using var sha = SHA256.Create();
        using var buffer = new MemoryStream();
        foreach (ClientTelemetryCommand command in ordered)
        {
            byte[] lineBytes = Encoding.UTF8.GetBytes($"{command.CommandId}{command.CommandType}");
            buffer.Write(lineBytes, 0, lineBytes.Length);
            buffer.Write(command.Payload, 0, command.Payload.Length);
        }

        byte[] digest = sha.ComputeHash(buffer.ToArray());
        var hex = new StringBuilder(digest.Length * 2);
        foreach (byte b in digest)
        {
            hex.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return hex.ToString(0, 16);
    }



    internal List<KeyValuePair<DateTimeOffset, TelemetrySnapshot>> CreateSnapshot()
    {
        var result = new List<KeyValuePair<DateTimeOffset, TelemetrySnapshot>>();
        if (!Enabled)
        {
            return result;
        }

        // O(1) swap the collectors out of the hot path, then build snapshots outside any lock (java #2081).
        ConcurrentDictionary<string, Collector> collectors = Interlocked.Exchange(ref _collectors, new ConcurrentDictionary<string, Collector>());
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long windowEnd = now;
        long windowStart;

        lock (_snapshotsLock)
        {
            windowStart = _lastSnapshotEnd > 0 ? _lastSnapshotEnd : now - HeartbeatIntervalMs;
            _lastSnapshotEnd = windowEnd;
        }

        // Capture the current collection scope so a collection disabled mid-window does not leak metrics into
        // the snapshot (Java filterCollectionMetrics applies the scope at snapshot time).
        bool allCollectionsEnabled;
        HashSet<string> enabledCollections;
        lock (_collectionsLock)
        {
            allCollectionsEnabled = _allCollectionsEnabled;
            enabledCollections = new HashSet<string>(_enabledCollections, StringComparer.Ordinal);
        }

        foreach (KeyValuePair<string, Collector> pair in collectors)
        {
            Collector collector = pair.Value;
            MetricsInfo global = collector.Global.Snapshot();
            ConcurrentDictionary<string, MetricBucket> collections = Interlocked.Exchange(ref collector.Collections, new ConcurrentDictionary<string, MetricBucket>());
            var collectionMetrics = new Dictionary<string, MetricsInfo>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, MetricBucket> bucketPair in collections)
            {
                if (allCollectionsEnabled || enabledCollections.Contains(bucketPair.Key))
                {
                    collectionMetrics[bucketPair.Key] = bucketPair.Value.Snapshot();
                }
            }

            var snapshot = new TelemetrySnapshot(
                pair.Key,
                DateTimeOffset.FromUnixTimeMilliseconds(Math.Max(windowStart, now - SnapshotHistoryTtlMs)),
                DateTimeOffset.FromUnixTimeMilliseconds(windowEnd),
                global,
                collectionMetrics,
                collector.Global.SnapshotSamples());

            result.Add(new KeyValuePair<DateTimeOffset, TelemetrySnapshot>(DateTimeOffset.FromUnixTimeMilliseconds(windowEnd), snapshot));
        }

        lock (_snapshotsLock)
        {
            _snapshots.AddRange(result);
            if (_snapshots.Count > SnapshotHardCap)
            {
                _snapshots.RemoveRange(0, _snapshots.Count - SnapshotHardCap);
            }

            DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddMilliseconds(-SnapshotHistoryTtlMs);
            _snapshots.RemoveAll(pair => pair.Key < cutoff);
        }

        return result;
    }

    private long _lastSnapshotEnd;

    private static Grpc.OperationMetrics ToGrpcOperationMetrics(TelemetrySnapshot snapshot)
    {
        var metrics = new Grpc.OperationMetrics { Operation = snapshot.Operation };
        metrics.Global = ToGrpcMetrics(snapshot.Global);
        foreach (KeyValuePair<string, MetricsInfo> pair in snapshot.CollectionMetrics)
        {
            metrics.CollectionMetrics[pair.Key] = ToGrpcMetrics(pair.Value);
        }

        return metrics;
    }

    private static Grpc.Metrics ToGrpcMetrics(MetricsInfo metrics)
        => new()
        {
            RequestCount = metrics.RequestCount,
            SuccessCount = metrics.SuccessCount,
            ErrorCount = metrics.ErrorCount,
            AvgLatencyMs = metrics.AvgLatencyMs,
            P99LatencyMs = metrics.P99LatencyMs,
            MaxLatencyMs = metrics.MaxLatencyMs
        };

    private static Grpc.CommandReply ToGrpcCommandReply(ClientTelemetryCommandReply reply)
        => new()
        {
            CommandId = reply.CommandId,
            Success = reply.Success,
            ErrorMessage = reply.ErrorMessage,
            Payload = reply.Payload is null ? ByteString.Empty : ByteString.CopyFrom(reply.Payload)
        };

    private sealed class MetricBucket
    {
        private readonly object _lock = new();
        private readonly double[] _samples = new double[SampleRingSize];
        private int _sampleCount;
        private int _sampleIndex;
        private long _requestCount;
        private long _successCount;
        private long _errorCount;
        private double _totalLatencyMicros;
        private double _maxLatencyMicros;

        internal void Record(double latencyMicros, bool success)
        {
            lock (_lock)
            {
                _requestCount++;
                if (success)
                {
                    _successCount++;
                }
                else
                {
                    _errorCount++;
                }

                _totalLatencyMicros += latencyMicros;
                if (latencyMicros > _maxLatencyMicros)
                {
                    _maxLatencyMicros = latencyMicros;
                }

                _samples[_sampleIndex] = latencyMicros;
                _sampleIndex = (_sampleIndex + 1) % _samples.Length;
                if (_sampleCount < _samples.Length)
                {
                    _sampleCount++;
                }
            }
        }

        internal MetricsInfo Snapshot()
        {
            lock (_lock)
            {
                double avg = _requestCount == 0 ? 0 : _totalLatencyMicros / 1000 / _requestCount;
                return new MetricsInfo(
                    _requestCount, _successCount, _errorCount,
                    avg, Percentile99Locked() / 1000, _maxLatencyMicros / 1000);
            }
        }

        internal double[] SnapshotSamples()
        {
            lock (_lock)
            {
                var samples = new double[_sampleCount];
                Array.Copy(_samples, samples, _sampleCount);
                return samples;
            }
        }

        private double Percentile99Locked()
        {
            if (_sampleCount == 0)
            {
                return 0;
            }

            var sorted = new double[_sampleCount];
            Array.Copy(_samples, sorted, _sampleCount);
            Array.Sort(sorted);
            int index = (int)Math.Ceiling(_sampleCount * 0.99) - 1;
            return sorted[Math.Max(0, index)];
        }
    }

    private sealed class Collector
    {
        internal readonly MetricBucket Global = new();
        internal ConcurrentDictionary<string, MetricBucket> Collections = new();
    }
}
