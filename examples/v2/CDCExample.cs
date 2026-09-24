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

        GetReplicateConfigurationResp config = await client.GetReplicateConfigurationAsync(new GetReplicateConfigurationReq());
        Console.WriteLine($"Replication configured for clusters: " +
            string.Join(", ", config.Configuration?.Clusters.Select(c => c.ClusterId) ?? []));

        GetReplicateInfoResp info = await client.GetReplicateInfoAsync(new GetReplicateInfoReq
        {
            SourceClusterId = clusterAId,
            TargetPchannel = pchannelsA[0]
        });
        Console.WriteLine($"Replicate info: checkpoint={info.Checkpoint?.Pchannel}, salvage={info.SalvageCheckpoint?.Pchannel}");

        Console.WriteLine("Done.");
    }
}
