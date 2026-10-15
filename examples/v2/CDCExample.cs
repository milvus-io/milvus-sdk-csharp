using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Utility;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates cross-cluster replication (CDC): configure the replication topology and read the
/// replication state. Mirrors java CDCExample and cpp examples/src/v2/cdc.cpp.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show <c>UpdateReplicateConfigurationAsync</c>,
/// <c>GetReplicateConfigurationAsync</c> and <c>GetReplicateInfoAsync</c>.</para>
/// <para><b>APIs used:</b> <c>UpdateReplicateConfigurationAsync</c>, <c>GetReplicateConfigurationAsync</c>,
/// <c>GetReplicateInfoAsync</c>.</para>
/// <para><b>Expected output:</b> replication configuration reported, then "Done.".</para>
/// </remarks>
public static class CDCExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        // A full replication demo needs two Milvus instances on distinct URIs. Point MILVUS_URI_B at the
        // second instance; when it is not set (or equals the primary) the example stops after describing
        // the topology, because the server rejects a topology that maps two cluster ids to one URI.
        string uriB = Environment.GetEnvironmentVariable("MILVUS_URI_B") ?? "";
        if (string.IsNullOrEmpty(uriB) || uriB == uri)
        {
            Console.WriteLine("CDC needs a second Milvus instance; set MILVUS_URI_B to its URI.");
            Console.WriteLine("Done.");
            return;
        }

        const string clusterAId = "cdc-a";
        const string clusterBId = "cdc-b";

        var pchannelsA = Enumerable.Range(0, 16).Select(i => $"{clusterAId}-rootcoord-dml_{i}").ToArray();
        var pchannelsB = Enumerable.Range(0, 16).Select(i => $"{clusterBId}-rootcoord-dml_{i}").ToArray();

        Console.WriteLine("Configuring replication...");
        #region Snippet:MilvusCdc_Configure
        await client.UpdateReplicateConfigurationAsync(new UpdateReplicateConfigurationReq
        {
            ReplicateConfiguration = new ReplicateConfiguration(
                [
                    new MilvusCluster(clusterAId, uri, pchannels: pchannelsA),
                    new MilvusCluster(clusterBId, uriB, pchannels: pchannelsB)
                ],
                [new CrossClusterTopology(clusterAId, clusterBId)])
        });
        #endregion

        Console.WriteLine("Getting replication configuration...");
        GetReplicateConfigurationResp config = await client.GetReplicateConfigurationAsync(new GetReplicateConfigurationReq());
        Console.WriteLine($"Replication configured for clusters: " +
            string.Join(", ", config.Configuration?.Clusters.Select(c => c.ClusterId) ?? []));

        // GetReplicateInfoAsync operates on a channel of the *target* cluster, so it must be issued on the
        // cluster-B client with a cluster-B pchannel (mirroring the C++ cdc.cpp).
        using MilvusClientV2 clientB = ExampleHelpers.CreateClient(uriB);
        Console.WriteLine("Connecting to cluster B...");
        await clientB.ConnectAsync();

        // Mirror the C++/Java demos by confirming the topology from the target cluster too.
        Console.WriteLine("Confirming replication configuration on cluster B...");
        await clientB.UpdateReplicateConfigurationAsync(new UpdateReplicateConfigurationReq
        {
            ReplicateConfiguration = new ReplicateConfiguration(
                [
                    new MilvusCluster(clusterAId, uri, pchannels: pchannelsA),
                    new MilvusCluster(clusterBId, uriB, pchannels: pchannelsB)
                ],
                [new CrossClusterTopology(clusterAId, clusterBId)])
        });

        Console.WriteLine("Getting replication info...");
        GetReplicateInfoResp info = await clientB.GetReplicateInfoAsync(new GetReplicateInfoReq
        {
            SourceClusterId = clusterAId,
            TargetPchannel = pchannelsB[0]
        });
        Console.WriteLine($"Replicate info: checkpoint={info.Checkpoint?.Pchannel}, salvage={info.SalvageCheckpoint?.Pchannel}");

        // DumpMessages streams messages from a physical channel starting at the salvage checkpoint's message
        // id (mirroring the C++ cdc.cpp dump step); consume a bounded number of messages, then cancel.
        Console.WriteLine("Dumping messages...");
        string dumpStart = info.SalvageCheckpoint?.MessageId ?? "0";
        var startId = new MessageID(dumpStart, info.SalvageCheckpoint?.WalName ?? WalName.RocksMq);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        int dumped = 0;
        try
        {
            await foreach (DumpMessageInfo message in clientB.DumpMessages(new DumpMessagesReq
            {
                Pchannel = pchannelsB[0],
                StartMessageId = startId
            }).WithCancellation(cts.Token))
            {
                dumped++;
                if (dumped >= 10)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The bounded stream was cancelled after the timeout; the dump is complete.
        }

        Console.WriteLine($"Dumped {dumped} messages");

        Console.WriteLine("Done.");
    }
}
