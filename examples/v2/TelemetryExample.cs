using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates the client-side telemetry manager: it records per-operation metrics, reports them to the server
/// over a heartbeat loop, and exposes them locally through <c>GetTelemetry()</c>.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show <see cref="MilvusClientV2.GetTelemetry" /> / <see cref="ClientTelemetryManager" /> —
/// the client reports a heartbeat carrying its id and per-operation metrics, and callers can inspect the
/// collected snapshots and recent errors.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>InsertAsync</c>, <c>SearchAsync</c>,
/// <c>GetTelemetry()</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> the client id / readiness, a heartbeat state line, and a metrics snapshot once
/// a heartbeat window elapses, then "Done.".</para>
/// </remarks>
public static class TelemetryExample
{
    public static async Task Run(string uri)
    {
        // Use a short heartbeat interval so the example produces a snapshot quickly (the default is 10s).
        using MilvusClientV2 client = ExampleHelpers.CreateClient(
            uri, "root:Milvus", new TelemetryConfig { HeartbeatIntervalMs = 1000 });
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        ClientTelemetryManager telemetry = client.GetTelemetry();
        Console.WriteLine($"client_id={telemetry.ClientId} ready={telemetry.IsReady} supported={telemetry.IsSupported}");

        const string collectionName = "telemetry_example";
        const int dimension = 4;
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("vector", dimension: dimension)
                }
            }
        });

        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [new IndexParam("vector", null, IndexType.Flat, MetricType.L2)]
        });

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            RowsData = new List<IDictionary<string, object?>>
            {
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["vector"] = new[] { 0.1f, 0.2f, 0.3f, 0.4f }
                }
            }
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "vector",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f, 0.3f, 0.4f }) },
            MetricType = MetricType.L2,
            Limit = 1
        });

        // Let a heartbeat window elapse so the recorded operations are flushed into a snapshot.
        Console.WriteLine("Waiting for a telemetry heartbeat window...");
        await Task.Delay(2500);

        Console.WriteLine($"supported={telemetry.IsSupported} last_heartbeat_error={telemetry.LastHeartbeatError ?? "<none>"}");
        foreach (TelemetrySnapshot snapshot in telemetry.MetricsSnapshots())
        {
            // Collection-backed operations are recorded in both the global and the per-collection bucket, so the
            // global count already includes the per-collection counts; sum only the global bucket to avoid double counting.
            Console.WriteLine($"  operation={snapshot.Operation} requests={snapshot.Global.RequestCount} avg_ms={snapshot.Global.AvgLatencyMs:F3}");
        }

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
