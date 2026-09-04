using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Utility;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates collection optimization: wait for indexes, trigger a major compaction with a
/// target segment size, and inspect segment information before/after.
/// Mirrors cpp examples/src/v2/optimize.cpp and java OptimizeExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show <c>OptimizeAsync</c> to merge segments into a target size and the
/// segment-info query to observe the result.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>,
/// <c>InsertAsync</c>, <c>OptimizeAsync</c>, <c>GetQuerySegmentInfoAsync</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Optimize status: success", then "Done.".</para>
/// </remarks>
public static class OptimizeExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "optimize_example";
        const int dimension = 8;

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
            Indexes = [new IndexParam("vector", null, IndexType.Flat, SimilarityMetricType.L2)]
        });

        // Insert enough rows that compaction has something to merge.
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            ColumnsData =
            [
                FieldData.Create("id", Enumerable.Range(0, 10_000).Select(i => (long)i).ToArray()),
                FieldData.CreateFloatVector("vector", Enumerable.Range(0, 10_000)
                    .Select(i => new ReadOnlyMemory<float>(new[] { i % 9 / 10f, i % 7 / 10f, i % 5 / 10f, i % 3 / 10f, 0.1f, 0.2f, 0.3f, 0.4f }))
                    .ToArray())
            ]
        });

        GetQuerySegmentInfoResp before = await client.GetQuerySegmentInfoAsync(
            new GetQuerySegmentInfoReq { CollectionName = collectionName });
        Console.WriteLine($"Segments before optimize: {before.Infos.Count}");

        #region Snippet:MilvusOptimize_Run
        OptimizeResp result = await client.OptimizeAsync(new OptimizeReq
        {
            CollectionName = collectionName,
            TargetSize = "1GB",
            WaitForCompletion = true,
            TimeoutMilliseconds = 120_000
        });
        Console.WriteLine($"Optimize status: {result.Status}; compactionId={result.CompactionId}");
        #endregion

        GetQuerySegmentInfoResp after = await client.GetQuerySegmentInfoAsync(
            new GetQuerySegmentInfoReq { CollectionName = collectionName });
        Console.WriteLine($"Segments after optimize: {after.Infos.Count}");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
