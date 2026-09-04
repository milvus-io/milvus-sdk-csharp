using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates geometry fields: insert WKT strings (POINT/LINESTRING/POLYGON) and filter
/// with spatial predicates (ST_EQUALS, ST_WITHIN, ...).
/// Mirrors cpp examples/src/v2/geometry_field.cpp and java GeometryExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to define a <c>Geometry</c> field, insert GeoJSON/WKT values,
/// and query with spatial-expression filters.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c> (Geometry field),
/// <c>InsertAsync</c>, <c>QueryAsync</c> with ST_* filters, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Points within rectangle: 2", then "Done.".</para>
/// </remarks>
public static class GeometryExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "geometry_example";

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    new FieldSchema("geo", DataType.Geometry),
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }
            }
        });

        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [new IndexParam("vector", null, IndexType.Flat, SimilarityMetricType.L2)]
        });

        #region Snippet:MilvusGeometry_Insert
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            RowsData =
            [
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["geo"] = "POINT(1 1)",
                    ["vector"] = new[] { 0.1f, 0.1f }
                },
                new Dictionary<string, object?>
                {
                    ["id"] = 2L,
                    ["geo"] = "POINT(5 5)",
                    ["vector"] = new[] { 0.5f, 0.5f }
                },
                new Dictionary<string, object?>
                {
                    ["id"] = 3L,
                    ["geo"] = "POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))",
                    ["vector"] = new[] { 0.9f, 0.9f }
                }
            ]
        });
        #endregion

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        QueryResp query = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "ST_WITHIN(geo, 'POLYGON((0 0, 0 6, 6 6, 6 0, 0 0))')",
            Parameters = new QueryParameters { OutputFields = { "id" } }
        });
        Console.WriteLine($"Points within rectangle: {query.FieldsData.FirstOrDefault()?.RowCount ?? 0}");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
