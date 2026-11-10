using System.Diagnostics;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Responses.Utility;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Lists the Search, HybridSearch and Query requests running in a cluster, and cancels one by id.
/// Mirrors java RunningRequestsExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how an operator can observe the requests a cluster is currently serving
/// (<c>ListRunningRequestsAsync</c>) and stop one of them (<c>CancelRequestsAsync</c>). The example starts a
/// deliberately heavy search (many query vectors, brute-force FLAT index) in a background thread, finds it with
/// <c>ListRunningRequestsAsync</c>, cancels it with <c>CancelRequestsAsync</c>, and shows that the search's own
/// client fails with error code 3002 (request canceled).</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>, <c>InsertAsync</c>,
/// <c>ListRunningRequestsAsync</c>, <c>CancelRequestsAsync</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> the running requests and per-proxy outcomes, the found/canceled search, and the
/// search client's 3002 error, then "Done.".</para>
/// <para>Requires a Milvus server that supports <c>ListRunningRequests</c> / <c>CancelRequests</c> (Milvus 3.0+).
/// With authorization enabled, listing and canceling other users' requests requires the
/// <c>ListRunningRequests</c> / <c>CancelRequests</c> privileges; any user may list and cancel their own.</para>
/// </remarks>
public static class RunningRequestsExample
{
    private const string CollectionName = "csharp_sdk_example_running_requests";
    private const int Dimension = 128;
    private const int RowCount = 100000;
    private const int Nq = 5000;
    private const string VectorFieldName = "vector";

    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        await PrepareCollectionAsync(client);
        await ListAllAsync(client);
        await CancelHeavySearchAsync(client);
        await CancelUnknownIdAsync(client);

        Console.WriteLine("Dropping collection...");
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = CollectionName });
        Console.WriteLine("Done.");
    }

    private static async Task PrepareCollectionAsync(MilvusClientV2 client)
    {
        await ExampleHelpers.ResetCollectionAsync(client, CollectionName);

        // A FLAT index makes every search a brute-force scan, so a search with many query vectors runs long
        // enough to be seen and canceled.
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = CollectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector(VectorFieldName, dimension: Dimension)
                }
            }
        });

        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = CollectionName,
            Indexes = [new IndexParam(VectorFieldName, null, IndexType.Flat, MetricType.L2)]
        });

        var random = new Random(42);
        const int batch = 5000;
        for (int start = 0; start < RowCount; start += batch)
        {
            var rows = new List<IDictionary<string, object?>>(batch);
            for (int i = start; i < start + batch; i++)
            {
                rows.Add(new Dictionary<string, object?>
                {
                    ["id"] = (long)i,
                    [VectorFieldName] = RandomVector(random)
                });
            }

            MutationResp resp = await client.InsertAsync(new InsertReq
            {
                CollectionName = CollectionName,
                RowsData = rows
            });
            Console.WriteLine($"Inserted {resp.InsertCount} rows ({start + batch} total)");
        }
    }

    private static void PrintNodeResults(IReadOnlyList<RunningRequestNodeResult> nodeResults)
    {
        foreach (RunningRequestNodeResult node in nodeResults)
        {
            if (node.IsSuccess)
            {
                Console.WriteLine($"  proxy {node.NodeId} answered");
            }
            else
            {
                Console.WriteLine($"  proxy {node.NodeId} did not answer (unimplemented={node.Unimplemented}): {node.Reason}");
            }
        }
    }

    private static async Task ListAllAsync(MilvusClientV2 client)
    {
        ListRunningRequestsResp resp = await client.ListRunningRequestsAsync(new ListRunningRequestsReq());
        Console.WriteLine($"{resp.Requests.Count} request(s) running in the cluster");
        PrintNodeResults(resp.NodeResults);
    }

    private static async Task CancelHeavySearchAsync(MilvusClientV2 client)
    {
        var random = new Random(7);
        var vectors = new ReadOnlyMemory<float>[Nq];
        for (int i = 0; i < Nq; i++)
        {
            vectors[i] = new ReadOnlyMemory<float>(RandomVector(random));
        }

        var outcome = new TaskCompletionSource<string>();
        Task searcher = Task.Run(() => SearchAsync(client, vectors, outcome));
        try
        {
            // Find the search by its collection, then cancel it by id.
            RunningRequestInfo? target = null;
            var stopwatch = Stopwatch.StartNew();
            while (target is null && !searcher.IsCompleted && stopwatch.ElapsedMilliseconds < 10000)
            {
                ListRunningRequestsResp listed = await client.ListRunningRequestsAsync(
                    new ListRunningRequestsReq { CollectionName = CollectionName });
                target = listed.Requests.FirstOrDefault(info => info.Type == "Search");
                if (target is null)
                {
                    await Task.Delay(20);
                }
            }

            if (target is null)
            {
                Console.WriteLine($"\nThe search was not seen running ({await outcome.Task}); raise NQ to make it slower");
                return;
            }

            Console.WriteLine($"\nFound the search: id={target.RequestId} user={target.User} nq={target.Nq} state={target.State} elapsed={target.ElapsedMs}ms");

            CancelRequestsResp canceled = await client.CancelRequestsAsync(new CancelRequestsReq
            {
                RequestIds = new[] { target.RequestId },
                Reason = "canceled by RunningRequestsExample"
            });
            foreach (RunningRequestInfo info in canceled.Canceled)
            {
                Console.WriteLine($"Canceled {info.RequestId} after {info.ElapsedMs}ms (state before the cancel: {info.State})");
            }
            Console.WriteLine("Not found: " + string.Join(", ", canceled.NotFound));
            Console.WriteLine("Undetermined: " + string.Join(", ", canceled.Undetermined));

            Console.WriteLine("Search client: " + await outcome.Task);
        }
        finally
        {
            await searcher;
        }
    }

    private static async Task SearchAsync(
        MilvusClientV2 client, IReadOnlyList<ReadOnlyMemory<float>> vectors, TaskCompletionSource<string> outcome)
    {
        try
        {
            await client.SearchAsync(new SearchReq
            {
                CollectionName = CollectionName,
                Vectors = vectors,
                Limit = 100
            });
            outcome.TrySetResult("the search finished before it was canceled");
        }
        catch (MilvusException ex)
        {
            outcome.TrySetResult($"the search failed with error code {(int)ex.ErrorCode} ({ex.ErrorCode}): {ex.Message}");
        }
    }

    private static async Task CancelUnknownIdAsync(MilvusClientV2 client)
    {
        // An id that no proxy holds is reported in NotFound once every proxy has answered.
        CancelRequestsResp resp = await client.CancelRequestsAsync(new CancelRequestsReq
        {
            RequestIds = new[] { "0-0-1" }
        });
        Console.WriteLine($"\nCancel an unknown id, not found: {string.Join(", ", resp.NotFound)}");
    }

    private static float[] RandomVector(Random random)
    {
        var vector = new float[Dimension];
        for (int i = 0; i < Dimension; i++)
        {
            vector[i] = (float)random.NextDouble();
        }

        return vector;
    }
}
