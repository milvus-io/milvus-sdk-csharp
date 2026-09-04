using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates consistency levels, including Session consistency (read-your-writes via the ts cache).
/// Mirrors java ConsistencyLevelExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show the effect of <c>ConsistencyLevel</c> on queries and searches, and how
/// <c>Session</c> makes a client's own writes visible immediately.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>,
/// <c>InsertAsync</c>, <c>QueryAsync</c> / <c>SearchAsync</c> with a consistency level,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Session-consistency query returned 2 rows" and
/// "Strong-consistency search returned 2 hits", then "Done.".</para>
/// </remarks>
public static class ConsistencyLevelExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "consistency_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusConsistency_Session
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            ConsistencyLevel = ConsistencyLevel.BoundedStaleness,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }
            }
        });
        #endregion

        await client.CreateIndexAsync(new Milvus.Client.V2.Requests.Index.CreateIndexReq
        {
            CollectionName = collectionName,
            FieldName = "vector",
            IndexType = IndexType.Flat,
            MetricType = SimilarityMetricType.L2
        });

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            Data =
            [
                FieldData.Create("id", new long[] { 1, 2 }),
                FieldData.CreateFloatVector("vector", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                    new ReadOnlyMemory<float>(new[] { 0f, 1f })
                })
            ]
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        // Session consistency: this query sees the rows just inserted by this client.
        QueryResp sessionQuery = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id in [1, 2]",
            Parameters = new QueryParameters { ConsistencyLevel = ConsistencyLevel.Session }
        });
        Console.WriteLine($"Session-consistency query returned {sessionQuery.FieldsData.FirstOrDefault()?.RowCount ?? 0} rows");

        // Strong consistency.
        SearchResp search = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "vector",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 0f }) },
            MetricType = SimilarityMetricType.L2,
            Limit = 2,
            Parameters = new SearchParameters { ConsistencyLevel = ConsistencyLevel.Strong }
        });
        Console.WriteLine($"Strong-consistency search returned {search.Ids.LongIds?.Count ?? 0} hits");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
