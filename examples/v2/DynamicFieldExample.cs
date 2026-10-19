using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates dynamic fields. Mirrors cpp examples/src/v2/dynamic_field.cpp.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to store rows with fields that are not declared in the schema, by
/// enabling dynamic fields and tagging the extra columns <c>isDynamic: true</c>.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c> (with <c>EnableDynamicFields</c>),
/// <c>InsertAsync</c>, <c>QueryAsync</c> filtering on a dynamic field,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Rows returned: 1", then "Done.".</para>
/// </remarks>
public static class DynamicFieldExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        const string collectionName = "dynamic_field_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusDynamicField_Schema
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                },
                EnableDynamicFields = true
            }
        });
        #endregion

        // "unknown_varchar" and "unknown_int" are dynamic fields (not in the schema).
        Console.WriteLine("Inserting...");
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            ColumnsData =
            [
                FieldData.Create("id", new long[] { 1, 2 }),
                FieldData.CreateFloatVector("vector", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                    new ReadOnlyMemory<float>(new[] { 0f, 1f })
                }),
                FieldData.CreateVarChar("unknown_varchar", new[] { "x", "y" }, isDynamic: true),
                FieldData.Create("unknown_int", new long[] { 8, 9 }, isDynamic: true)
            ]
        });

        // A dynamic-field filter is a JSON-path filter on $meta, and since Milvus 2.4 the query node requires
        // an INVERTED index on that path (see JsonFieldExample); build it alongside the vector index.
        Console.WriteLine("Creating index...");
        // Sync=false: $meta is a reserved dynamic field, not a schema field, so the sync wait's
        // DescribeIndex(fieldName="$meta") finds no index to poll. Skip the wait; the LoadCollectionAsync below
        // gives the INVERTED index time to build before the dynamic-field query.
        await client.CreateIndexAsync(new Milvus.Client.V2.Requests.Index.CreateIndexReq
        {
            CollectionName = collectionName,
            Sync = false,
            Indexes =
            [
                new Milvus.Client.V2.Types.IndexParam("vector", null, IndexType.Flat, MetricType.L2),
                // Dynamic fields live in the server-side "$meta" column; the INVERTED index must be created on
                // that reserved field (not a literal "meta" field) with a json_path covering the filtered key
                // (unknown_int), plus the mandatory json_cast_type extra param; DOUBLE matches the numeric
                // comparison on unknown_int.
                new Milvus.Client.V2.Types.IndexParam("$meta", "meta_idx", IndexType.Inverted, null)
                {
                    ExtraParams = { ["json_cast_type"] = "DOUBLE", ["json_path"] = "unknown_int" }
                }
            ]
        });

        Console.WriteLine("Loading collection...");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        Console.WriteLine("Querying...");
        QueryResp results = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "unknown_int > 8",
            Parameters = new QueryParameters { OutputFields = { "*" } }
        });
        Console.WriteLine($"Rows returned: {results.FieldsData.FirstOrDefault()?.RowCount ?? 0}");
        ExampleHelpers.PrintQueryRows(results);

        Console.WriteLine("Dropping collection...");
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
