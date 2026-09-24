using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates a JSON field: schema with a JSON field, insert JSON rows, filter on a JSON key.
/// Mirrors cpp examples/src/v2/json.cpp and java JsonFieldExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to store semi-structured rows in a <c>Json</c> column and filter on
/// a key inside the JSON document.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>InsertAsync</c> (with
/// <c>CreateJson</c>), <c>QueryAsync</c> filtering with a JSON path expression,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Rows matching electronics: 2", then "Done.".</para>
/// </remarks>
public static class JsonFieldExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "json_field_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusJsonField_Insert
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    new FieldSchema("meta", DataType.Json),
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }
            }
        });

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            ColumnsData =
            [
                FieldData.Create("id", new long[] { 1, 2, 3 }),
                FieldData.CreateJson("meta", new[]
                {
                    "{\"category\": \"electronics\", \"price\": 99}",
                    "{\"category\": \"books\", \"price\": 12}",
                    "{\"category\": \"electronics\", \"price\": 199}"
                }),
                FieldData.CreateFloatVector("vector", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                    new ReadOnlyMemory<float>(new[] { 0f, 1f }),
                    new ReadOnlyMemory<float>(new[] { 1f, 1f })
                })
            ]
        });
        #endregion

        await client.CreateIndexAsync(new Milvus.Client.V2.Requests.Index.CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [ new Milvus.Client.V2.Types.IndexParam("vector", null, IndexType.Flat, SimilarityMetricType.L2) ]
        });

        // Filtering on a JSON field path requires an INVERTED index on the JSON field since Milvus 2.4,
        // otherwise the query node reports "Cannot find json index with path". The index needs an explicit
        // name: index names are global within a collection, so leaving it unset would collide with the
        // vector index's default `_default_idx` (at most one index is allowed per field+name).
        await client.CreateIndexAsync(new Milvus.Client.V2.Requests.Index.CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [ new Milvus.Client.V2.Types.IndexParam("meta", "meta_idx", IndexType.Inverted, null) ]
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        QueryResp results = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "meta[\"category\"] == \"electronics\"",
            Parameters = new QueryParameters { OutputFields = { "id", "meta" } }
        });
        Console.WriteLine($"Rows matching electronics: {results.FieldsData.FirstOrDefault()?.RowCount ?? 0}");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
