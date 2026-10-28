using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Types;

[Trait("Category", "Unit")]
public class ClientTelemetryTests
{
    private static ClientTelemetryManager CreateManager(TelemetryConfig? config = null, string? clientId = null)
        => new(
            config ?? new TelemetryConfig { ClientId = clientId },
            grpcClient: null,
            username: () => "root",
            database: () => "default",
            sdkVersion: () => "1.0.0");

    private static Milvus.Client.Grpc.ClientCommand Command(string commandId, string commandType, bool persistent = false, long createTime = 1)
        => Command(commandId, commandType, "{}", persistent, createTime);

    private static Milvus.Client.Grpc.ClientCommand Command(string commandId, string commandType, string payload, bool persistent = false, long createTime = 1)
        => new()
        {
            CommandId = commandId,
            CommandType = commandType,
            Payload = Google.Protobuf.ByteString.CopyFromUtf8(payload),
            CreateTime = createTime,
            Persistent = persistent
        };

    private static Google.Protobuf.Collections.RepeatedField<Milvus.Client.Grpc.ClientCommand> Commands(params Milvus.Client.Grpc.ClientCommand[] items)
    {
        var repeated = new Google.Protobuf.Collections.RepeatedField<Milvus.Client.Grpc.ClientCommand>();
        foreach (Milvus.Client.Grpc.ClientCommand item in items)
        {
            repeated.Add(item);
        }

        return repeated;
    }

    [Fact]
    public void TelemetryConfig_normalizes_values()
    {
        var config = new TelemetryConfig
        {
            HeartbeatIntervalMs = 0,
            SamplingRate = double.NaN,
            ErrorMaxCount = 0
        }.Normalized();

        Assert.Equal(10_000, config.HeartbeatIntervalMs);
        Assert.Equal(0, config.SamplingRate);
        Assert.Equal(100, config.ErrorMaxCount);
    }

    [Fact]
    public void ClientId_generated_when_empty_and_stable_when_set()
    {
        using var generated = CreateManager(new TelemetryConfig { ClientId = "" });
        Assert.False(string.IsNullOrEmpty(generated.ClientId));

        using var stable = CreateManager(new TelemetryConfig { ClientId = "my-client" });
        Assert.Equal("my-client", stable.ClientId);
    }

    [Fact]
    public void RecordOperation_accumulates_metrics_into_snapshot()
    {
        using var manager = CreateManager(new TelemetryConfig { SamplingRate = 1.0 });

        // Like the references, per-collection buckets are disabled until a collection_metrics command enables them.
        manager.ProcessCommands(Commands(Command("cmd-1", "collection_metrics", """{"enabled":true,"collections":["*"]}""")));

        manager.RecordOperation("Search", "book", 1000, true, null);
        manager.RecordOperation("Search", "book", 2000, true, null);
        manager.RecordOperation("Search", "book", 3000, false, "boom");
        manager.RecordOperation("Insert", null, 500, true, null);

        var snapshots = manager.CreateSnapshot();

        // Like the Java reference, an operation with a collection updates BOTH the global and the per-collection
        // bucket; an operation without a collection updates only the global bucket.
        var search = snapshots.Select(pair => pair.Value).Single(s => s.Operation == "Search");
        Assert.Equal(3, search.Global.RequestCount);
        Assert.Equal(2, search.Global.SuccessCount);
        Assert.Equal(1, search.Global.ErrorCount);
        Assert.Equal(2.0, search.Global.AvgLatencyMs, precision: 3);
        Assert.Equal(3.0, search.Global.MaxLatencyMs, precision: 3);
        Assert.True(search.Global.P99LatencyMs > 0);
        MetricsInfo book = search.CollectionMetrics["book"];
        Assert.Equal(3, book.RequestCount);
        Assert.Equal(2, book.SuccessCount);
        Assert.Equal(1, book.ErrorCount);

        var insert = snapshots.Select(pair => pair.Value).Single(s => s.Operation == "Insert");
        Assert.Equal(1, insert.Global.RequestCount);
        Assert.Empty(insert.CollectionMetrics);
    }

    [Fact]
    public void Collection_metrics_disabled_by_default()
    {
        using var manager = CreateManager(new TelemetryConfig { SamplingRate = 1.0 });

        manager.RecordOperation("Search", "book", 1000, true, null);

        var snapshots = manager.CreateSnapshot();

        // Like the references, no collection_metrics command has been received yet, so only the global
        // bucket is populated.
        var search = snapshots.Select(pair => pair.Value).Single(s => s.Operation == "Search");
        Assert.Equal(1, search.Global.RequestCount);
        Assert.Empty(search.CollectionMetrics);
    }

    [Fact]
    public void RecentErrors_bounded_and_record_failures()
    {
        using var manager = CreateManager(new TelemetryConfig { SamplingRate = 1.0, ErrorMaxCount = 2 });

        manager.RecordOperation("Search", "book", 1, false, "error-1");
        manager.RecordOperation("Search", "book", 1, false, "error-2");
        manager.RecordOperation("Search", "book", 1, false, "error-3");

        IReadOnlyList<TelemetryError> errors = manager.RecentErrors();
        Assert.Equal(2, errors.Count);
        Assert.Equal("error-3", errors[0].ErrorMessage);
        Assert.Equal("error-2", errors[1].ErrorMessage);
    }

    [Fact]
    public void Sampling_rate_zero_records_nothing()
    {
        using var manager = CreateManager(new TelemetryConfig { SamplingRate = 0 });

        // The ShouldSample short-circuit (rate <= 0) must keep recording nothing across many calls,
        // not merely a single one.
        for (int i = 0; i < 10_000; i++)
        {
            manager.RecordOperation("Search", "book", 1000, true, null);
        }

        var snapshots = manager.CreateSnapshot();

        Assert.Empty(snapshots);
    }

    [Fact]
    public void Collection_metrics_enable_merges_into_enabled_set()
    {
        using var manager = CreateManager(new TelemetryConfig { SamplingRate = 1.0 });

        // Disable collection metrics entirely, then enable a specific list.
        manager.ProcessCommands(Commands(Command("cmd-1", "collection_metrics", """{"enabled":false,"collections":[]}""")));
        manager.ProcessCommands(Commands(Command("cmd-2", "collection_metrics", """{"enabled":true,"collections":["a"]}""")));

        // A second enable with a different list merges into the existing set instead of replacing it.
        manager.ProcessCommands(Commands(Command("cmd-3", "collection_metrics", """{"enabled":true,"collections":["b"]}""")));

        manager.RecordOperation("Search", "a", 1000, true, null);
        manager.RecordOperation("Search", "b", 1000, true, null);
        manager.RecordOperation("Search", "c", 1000, true, null);

        var search = manager.CreateSnapshot().Select(pair => pair.Value).Single(s => s.Operation == "Search");
        Assert.True(search.CollectionMetrics.ContainsKey("a"));
        Assert.True(search.CollectionMetrics.ContainsKey("b"));
        Assert.False(search.CollectionMetrics.ContainsKey("c"));
    }

    [Fact]
    public void Collection_metrics_empty_list_disable_turns_all_off()
    {
        using var manager = CreateManager(new TelemetryConfig { SamplingRate = 1.0 });

        // enabled=false with an empty collections list disables collection metrics entirely.
        manager.ProcessCommands(Commands(Command("cmd-1", "collection_metrics", """{"enabled":false,"collections":[]}""")));

        manager.RecordOperation("Search", "book", 1000, true, null);

        var search = manager.CreateSnapshot().Select(pair => pair.Value).Single(s => s.Operation == "Search");
        Assert.Empty(search.CollectionMetrics);
        Assert.Equal(1, search.Global.RequestCount);
    }

    [Fact]
    public void ConfigHash_deterministic_over_persistent_commands()
    {
        using var manager = CreateManager(new TelemetryConfig { ClientId = "c1" });

        manager.ProcessCommands(Commands(
            Command("b", "push_config", persistent: true),
            Command("a", "push_config", persistent: true)));

        string hash1 = manager.ConfigHash!;
        Assert.False(string.IsNullOrEmpty(hash1));
        Assert.Equal(16, hash1.Length);

        // Order-independent and deterministic.
        manager.ProcessCommands(Commands(
            Command("a", "push_config", persistent: true, createTime: 3),
            Command("b", "push_config", persistent: true, createTime: 3)));
        Assert.Equal(hash1, manager.ConfigHash);
    }

    [Fact]
    public void GetConfig_command_advances_command_cursor()
    {
        using var manager = CreateManager(new TelemetryConfig { ClientId = "c1", SamplingRate = 0.5 });

        manager.ProcessCommands(Commands(Command("cmd-1", "get_config")));

        Assert.Equal(1, manager.LastCommandTimestamp);
    }

    [Fact]
    public void Dispose_is_idempotent()
    {
        var manager = CreateManager(new TelemetryConfig { ClientId = "c1" });

        // Double-dispose must be a no-op (Java close() uses a compareAndSet guard); the manager is publicly
        // returned by GetTelemetry() and the owning client also disposes it.
        manager.Dispose();
        manager.Dispose();
    }

    [Fact]
    public void Stale_command_below_cursor_is_acked()
    {
        using var manager = CreateManager(new TelemetryConfig { ClientId = "c1" });

        // First batch advances the cursor to createTime=5.
        manager.ProcessCommands(Commands(Command("cmd-1", "get_config", createTime: 5)));

        // A redelivered command older than the cursor must still receive an empty success ACK (Java/C++/Rust
        // queue successReply(commandId, EMPTY)), so the server-side command is resolved instead of pending.
        // No exception may escape and the cursor must remain at the newest timestamp.
        manager.ProcessCommands(Commands(Command("cmd-2", "push_config", createTime: 1)));

        Assert.Equal(5, manager.LastCommandTimestamp);
    }

    [Fact]
    public void Snapshots_carry_retained_latency_samples()
    {
        using var manager = CreateManager(new TelemetryConfig { SamplingRate = 1.0 });

        manager.RecordOperation("Search", null, 10_000, true, null);
        manager.RecordOperation("Search", null, 20_000, true, null);
        manager.RecordOperation("Search", null, 90_000, true, null);

        var snapshot = manager.CreateSnapshot().Select(pair => pair.Value).Single(s => s.Operation == "Search");

        // The snapshot retains the raw global-bucket latency samples so the aggregate history can compute an
        // exact p99 across windows (Java/C++/Rust merge the retained samples rather than combining per-window p99s).
        Assert.Equal(3, snapshot.GlobalLatencySamplesMicros.Count);
        Assert.Contains(90_000.0, snapshot.GlobalLatencySamplesMicros);
    }
}
