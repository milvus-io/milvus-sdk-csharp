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
        await client.ConnectAsync();

        const string collectionName = "timestamp_example";

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

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

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

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

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
