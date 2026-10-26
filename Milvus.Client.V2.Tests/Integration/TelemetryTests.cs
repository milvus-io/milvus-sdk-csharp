using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class TelemetryTests
{
    // Create a connected client whose own background heartbeat is disabled, so the test drives heartbeats on
    // the test thread (the in-process TestServer does not service background-thread RPCs reliably) while reusing
    // the client's telemetry transport.
    private static async Task<(MockMilvusServer Server, MilvusClientV2 Client)> CreateAsync()
    {
        var server = new MockMilvusServer();
        var client = new MilvusClientV2(new ConnectConfig
        {
            Uri = server.Uri,
            ChannelOptions = server.ChannelOptions,
            Telemetry = new TelemetryConfig { Enabled = false }
        });
        await client.ConnectAsync(TestContext.Current.CancellationToken);
        return (server, client);
    }

    private static ClientTelemetryManager CreateManager(MockMilvusServer server, MilvusClientV2 client, string? clientId = null)
        => new(
            new TelemetryConfig { ClientId = clientId },
            client.TelemetryGrpcClient,
            () => "root",
            () => "default",
            () => "1.0.0");

    private static Milvus.Client.Grpc.ClientCommand Command(string commandId, string commandType, string payload)
        => Command(commandId, commandType, payload, createTime: 1);

    private static Milvus.Client.Grpc.ClientCommand Command(string commandId, string commandType, string payload, long createTime)
        => new()
        {
            CommandId = commandId,
            CommandType = commandType,
            Payload = Google.Protobuf.ByteString.CopyFromUtf8(payload),
            CreateTime = createTime
        };

    [Fact]
    public async Task Telemetry_heartbeat_reports_operation_metrics()
    {
        (MockMilvusServer server, MilvusClientV2 client) = await CreateAsync();
        using (server)
        using (client)
        using (var manager = CreateManager(server, client))
        {
            // Like the references, per-collection buckets are disabled until a collection_metrics command enables them.
            manager.ProcessCommands([Command("cmd-1", "collection_metrics", """{"enabled":true,"collections":["*"]}""")]);

            manager.RecordOperation("Search", "coll", 1500, true, null);

            await manager.SendHeartbeatAsync(manager.CreateSnapshot());

            Grpc.ClientHeartbeatRequest heartbeat = Assert.Single(server.TelemetryService.Heartbeats);
            Grpc.OperationMetrics search = Assert.Single(heartbeat.Metrics);
            Assert.Equal("Search", search.Operation);
            // The operation is recorded in both the global and the per-collection bucket (Java reference).
            Assert.Equal(1, search.Global.RequestCount);
            Assert.Equal(1, search.CollectionMetrics["coll"].RequestCount);
            Assert.Equal("CSharp", heartbeat.ClientInfo.SdkType);
            Assert.False(string.IsNullOrEmpty(heartbeat.ClientInfo.Reserved["client_id"]));
            Assert.Equal(heartbeat.ClientInfo.Reserved["client_id"], manager.ClientId);
        }
    }

    [Fact]
    public async Task Telemetry_processes_commands_and_replies()
    {
        (MockMilvusServer server, MilvusClientV2 client) = await CreateAsync();
        using (server)
        using (client)
        using (var manager = CreateManager(server, client, clientId: "stable-client"))
        {
            server.TelemetryService.HeartbeatCommandType = "get_config";

            // First heartbeat receives the get_config command; the manager processes it and queues a reply.
            await manager.SendHeartbeatAsync(manager.CreateSnapshot());
            Assert.Equal(1, manager.LastCommandTimestamp);

            // The next heartbeat delivers the reply.
            await manager.SendHeartbeatAsync(manager.CreateSnapshot());

            Grpc.ClientHeartbeatRequest last = server.TelemetryService.Heartbeats[^1];
            Grpc.CommandReply reply = Assert.Single(last.CommandReplies);
            Assert.True(reply.Success);
            Assert.StartsWith("cmd-", reply.CommandId);
            Assert.False(reply.Payload.IsEmpty);
        }
    }

    [Fact]
    public async Task Telemetry_push_config_reports_applied_and_ignored_keys()
    {
        (MockMilvusServer server, MilvusClientV2 client) = await CreateAsync();
        using (server)
        using (client)
        using (var manager = CreateManager(server, client, clientId: "stable-client"))
        {
            server.TelemetryService.HeartbeatCommandType = "push_config";
            server.TelemetryService.HeartbeatCommandPayload = """{"enabled":true,"heartbeat_interval_ms":5000,"sampling_rate":0.5,"ttl_seconds":60}""";

            // First heartbeat receives the push_config command; the manager processes it and queues a reply.
            await manager.SendHeartbeatAsync(manager.CreateSnapshot());

            // The next heartbeat delivers the reply.
            await manager.SendHeartbeatAsync(manager.CreateSnapshot());

            Grpc.ClientHeartbeatRequest last = server.TelemetryService.Heartbeats[^1];
            Grpc.CommandReply reply = Assert.Single(last.CommandReplies);
            Assert.True(reply.Success);

            using var doc = System.Text.Json.JsonDocument.Parse(reply.Payload.ToArray());
            string[] applied = doc.RootElement.GetProperty("applied").EnumerateArray().Select(x => x.GetString()!).ToArray();
            string[] ignored = doc.RootElement.GetProperty("ignored").EnumerateArray().Select(x => x.GetString()!).ToArray();

            Assert.Equal(["enabled", "heartbeat_interval_ms", "sampling_rate"], applied);
            // ttl_seconds is validated but never applied, so it is reported as ignored (Java reference).
            Assert.Equal(["ttl_seconds"], ignored);
        }
    }

    [Fact]
    public async Task Telemetry_push_config_fails_on_invalid_heartbeat_interval()
    {
        (MockMilvusServer server, MilvusClientV2 client) = await CreateAsync();
        using (server)
        using (client)
        using (var manager = CreateManager(server, client, clientId: "stable-client"))
        {
            server.TelemetryService.HeartbeatCommandType = "push_config";
            server.TelemetryService.HeartbeatCommandPayload = """{"heartbeat_interval_ms":0}""";

            await manager.SendHeartbeatAsync(manager.CreateSnapshot());
            await manager.SendHeartbeatAsync(manager.CreateSnapshot());

            Grpc.ClientHeartbeatRequest last = server.TelemetryService.Heartbeats[^1];
            Grpc.CommandReply reply = Assert.Single(last.CommandReplies);
            Assert.False(reply.Success);
            Assert.Contains("heartbeat_interval_ms", reply.ErrorMessage);
        }
    }

    [Fact]
    public async Task Telemetry_get_config_reports_user_config_shape()
    {
        (MockMilvusServer server, MilvusClientV2 client) = await CreateAsync();
        using (server)
        using (client)
        using (var manager = CreateManager(server, client, clientId: "stable-client"))
        {
            server.TelemetryService.HeartbeatCommandType = "get_config";

            await manager.SendHeartbeatAsync(manager.CreateSnapshot());
            await manager.SendHeartbeatAsync(manager.CreateSnapshot());

            Grpc.ClientHeartbeatRequest last = server.TelemetryService.Heartbeats[^1];
            Grpc.CommandReply reply = Assert.Single(last.CommandReplies);
            Assert.True(reply.Success);

            using var doc = System.Text.Json.JsonDocument.Parse(reply.Payload.ToArray());
            System.Text.Json.JsonElement userConfig = doc.RootElement.GetProperty("user_config");
            Assert.True(userConfig.TryGetProperty("telemetry_enabled", out _));
            Assert.True(userConfig.TryGetProperty("telemetry_heartbeat_interval_ms", out _));
            Assert.True(userConfig.TryGetProperty("telemetry_sampling_rate", out _));
            Assert.True(userConfig.TryGetProperty("address", out _));
            Assert.True(userConfig.TryGetProperty("username", out _));
            Assert.True(userConfig.TryGetProperty("db_name", out _));
            Assert.True(userConfig.TryGetProperty("enabled_collections", out _));
            Assert.True(userConfig.TryGetProperty("all_collections_enabled", out _));
            Assert.Equal("root", userConfig.GetProperty("username").GetString());
        }
    }

    [Fact]
    public async Task Telemetry_show_latency_history_requires_start_and_end()
    {
        (MockMilvusServer server, MilvusClientV2 client) = await CreateAsync();
        using (server)
        using (client)
        using (var manager = CreateManager(server, client, clientId: "stable-client"))
        {
            server.TelemetryService.HeartbeatCommandType = "show_latency_history";
            server.TelemetryService.HeartbeatCommandPayload = """{"start_time":"2026-01-01T00:00:00Z"}""";

            await manager.SendHeartbeatAsync(manager.CreateSnapshot());
            await manager.SendHeartbeatAsync(manager.CreateSnapshot());

            Grpc.ClientHeartbeatRequest last = server.TelemetryService.Heartbeats[^1];
            Grpc.CommandReply reply = Assert.Single(last.CommandReplies);
            Assert.False(reply.Success);
            Assert.Contains("start_time and end_time", reply.ErrorMessage);
        }
    }

    [Fact]
    public async Task Telemetry_acks_stale_command_below_cursor()
    {
        (MockMilvusServer server, MilvusClientV2 client) = await CreateAsync();
        using (server)
        using (client)
        using (var manager = CreateManager(server, client, clientId: "stable-client"))
        {
            // Process a command that advances the cursor to createTime=5, then process a redelivered
            // command older than the cursor (createTime=1). Both must be ACKed; the stale one with an empty
            // success reply, mirroring the Java/C++/Rust processCommands behavior.
            manager.ProcessCommands([
                Command("cmd-1", "get_config", """{"advance":true}""", createTime: 5),
                Command("cmd-2", "push_config", """{"enabled":true}""", createTime: 1)
            ]);

            // The next heartbeat delivers both replies.
            await manager.SendHeartbeatAsync(manager.CreateSnapshot());

            Grpc.ClientHeartbeatRequest last = server.TelemetryService.Heartbeats[^1];
            Assert.Equal(2, last.CommandReplies.Count);
            Assert.Contains(last.CommandReplies, reply => reply.CommandId == "cmd-1" && reply.Success);
            Assert.Contains(last.CommandReplies, reply => reply.CommandId == "cmd-2" && reply.Success);
        }
    }

    // A real facade call through InvokeAsync must record exactly one telemetry metric per logical operation,
    // and a schema-mismatch retry (which re-enters the facade) must not double-record. The client's own
    // telemetry manager is enabled so the InvokeAsync hook records into it; the heartbeat interval is set
    // very large so the background loop sleeps and the test drives CreateSnapshot on the test thread (the
    // in-process TestServer does not service background-thread RPCs reliably).
    private static async Task<(MockMilvusServer Server, MilvusClientV2 Client)> CreateFacadeAsync()
    {
        var server = new MockMilvusServer();
        var client = new MilvusClientV2(new ConnectConfig
        {
            Uri = server.Uri,
            ChannelOptions = server.ChannelOptions,
            Telemetry = new TelemetryConfig { Enabled = true, HeartbeatIntervalMs = 60 * 60 * 1000 }
        });
        await client.ConnectAsync(TestContext.Current.CancellationToken);
        return (server, client);
    }

    [Fact]
    public async Task Telemetry_real_facade_insert_records_one_metric()
    {
        (MockMilvusServer server, MilvusClientV2 client) = await CreateFacadeAsync();
        using (server)
        using (client)
        {
            SchemaCache.Instance.Clear();
            CollectionTsCache.Instance.Clear();

            await client.InsertAsync(new InsertReq
            {
                CollectionName = "facade_coll",
                ColumnsData =
                [
                    FieldData.Create("id", new long[] { 1, 2, 3 }),
                    FieldData.CreateVarChar("name", new[] { "a", "b", "c" })
                ]
            }, TestContext.Current.CancellationToken);

            // The client's own telemetry manager recorded exactly one Insert (the whole logical operation,
            // including any retry) into the global bucket.
            var snapshot = client.GetTelemetry().CreateSnapshot()
                .Select(pair => pair.Value).Single(s => s.Operation == "Insert");
            Assert.Equal(1, snapshot.Global.RequestCount);
            Assert.Equal(1, snapshot.Global.SuccessCount);
            Assert.Equal(0, snapshot.Global.ErrorCount);
        }
    }

    [Fact]
    public async Task Telemetry_real_facade_insert_schema_mismatch_retry_records_once()
    {
        (MockMilvusServer server, MilvusClientV2 client) = await CreateFacadeAsync();
        using (server)
        using (client)
        {
            SchemaCache.Instance.Clear();
            CollectionTsCache.Instance.Clear();

            // The first Insert RPC returns SchemaMismatch, so the facade re-enters InsertAsync and retries;
            // the logical operation must still be recorded exactly once with the final (success) outcome.
            server.Service.FailNextMutationsWithSchemaMismatch = 1;

            await client.InsertAsync(new InsertReq
            {
                CollectionName = "facade_coll",
                ColumnsData =
                [
                    FieldData.Create("id", new long[] { 1, 2, 3 }),
                    FieldData.CreateVarChar("name", new[] { "a", "b", "c" })
                ]
            }, TestContext.Current.CancellationToken);

            var snapshot = client.GetTelemetry().CreateSnapshot()
                .Select(pair => pair.Value).Single(s => s.Operation == "Insert");
            Assert.Equal(1, snapshot.Global.RequestCount);
            Assert.Equal(1, snapshot.Global.SuccessCount);
            Assert.Equal(0, snapshot.Global.ErrorCount);
        }
    }
}
