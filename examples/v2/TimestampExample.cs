using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates Timestamptz fields: insert ISO-8601 timestamps and query them, converting
/// results to the requested timezone via the <c>timezone</c> parameter.
/// Mirrors cpp examples/src/v2/timestamptz_field.cpp and java TimestampExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to store timezone-aware timestamps and retrieve them in a
/// different timezone.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c> (Timestamptz field),
/// <c>InsertAsync</c>, <c>QueryAsync</c> with <c>Parameters.Timezone</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Query in Asia/Shanghai returned N rows", then "Done.".</para>
/// </remarks>
public static class TimestampExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        const string collectionName = "timestamp_example";

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        Console.WriteLine("Creating collection...");
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    new FieldSchema("event_time", DataType.Timestamptz),
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }
            }
        });

        Console.WriteLine("Creating index...");
        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [new IndexParam("vector", null, IndexType.Flat, SimilarityMetricType.L2)]
        });

        #region Snippet:MilvusTimestamp_Insert
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            RowsData =
            [
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["event_time"] = "2025-01-01T00:00:00+08:00",
                    ["vector"] = new[] { 0.1f, 0.1f }
                },
                new Dictionary<string, object?>
                {
                    ["id"] = 2L,
                    ["event_time"] = "2025-01-02T12:30:00Z",
                    ["vector"] = new[] { 0.2f, 0.2f }
                }
            ]
        });
        #endregion

        Console.WriteLine("Loading collection...");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        Console.WriteLine("Querying...");
        QueryResp query = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id in [1, 2]",
            Parameters = new QueryParameters
            {
                OutputFields = { "id", "event_time" },
                Timezone = "Asia/Shanghai"
            }
        });
        Console.WriteLine($"Query in Asia/Shanghai returned {query.FieldsData.FirstOrDefault()?.RowCount ?? 0} row(s)");
        ExampleHelpers.PrintQueryRows(query);

        Console.WriteLine("Searching...");
        SearchResp search = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "vector",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.15f, 0.15f }) },
            MetricType = SimilarityMetricType.L2,
            Limit = 5,
            Parameters = new SearchParameters
            {
                OutputFields = { "id", "event_time" },
                Timezone = "Asia/Shanghai"
            }
        });
        Console.WriteLine($"Search in Asia/Shanghai returned {search.SingleResults?.FirstOrDefault()?.Scores.Count ?? 0} hit(s)");
        ExampleHelpers.PrintSearchHits(search);

        // Single-leg hybrid search, combining one ANN route with an RRF reranker; the timezone is set on the leg.
        Console.WriteLine("Hybrid searching...");
        SearchResp hybrid = await client.HybridSearchAsync(new HybridSearchReq
        {
            CollectionName = collectionName,
            SearchRequests =
            [
                new AnnSearchReq
                {
                    VectorFieldName = "vector",
                    Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.15f, 0.15f }) },
                    MetricType = SimilarityMetricType.L2,
                    Limit = 5,
                    Timezone = "Asia/Shanghai"
                }
            ],
            Reranker = new RrfReranker(10),
            Limit = 5,
            Parameters = new SearchParameters { OutputFields = { "id", "event_time" } }
        });
        Console.WriteLine($"Hybrid search returned {hybrid.SingleResults?.FirstOrDefault()?.Scores.Count ?? 0} hit(s)");
        ExampleHelpers.PrintSearchHits(hybrid);

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
