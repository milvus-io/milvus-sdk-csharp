using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates Array-of-Struct fields: a struct field with scalar and vector sub-fields,
/// inserted as lists of dictionaries, and searched on the nested vector sub-field via an
/// <see cref="EmbeddingList" />.
/// Mirrors cpp examples/src/v2/struct_field.cpp and java StructExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to define, insert and search over a struct field whose
/// sub-fields include a vector (searched via the <c>struct[sub]</c> field name with an
/// <see cref="EmbeddingList" /> and a MAX_SIM metric).</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c> (StructFields),
/// <c>InsertAsync</c>, <c>CreateIndexAsync</c>, <c>SearchAsync</c> with <c>EmbeddingLists</c>,
/// <c>AddCollectionStructFieldAsync</c>, <c>DescribeCollectionAsync</c>, <c>QueryAsync</c>,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Struct search returned N hits", then "Done.".</para>
/// </remarks>
public static class StructExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        const string collectionName = "struct_example";
        const int dimension = 4;

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusStruct_Create
        var structField = new StructFieldSchema("clips")
        {
            MaxCapacity = 8
        };
        structField.Fields.Add(new FieldSchema("int32", DataType.Int32));
        structField.Fields.Add(FieldSchema.CreateVarchar("varchar", maxLength: 100));
        structField.Fields.Add(new FieldSchema("vector", DataType.FloatVector) { Dimension = dimension });

        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true)
                },
                StructFields = { structField }
            }
        });
        #endregion

        // Indexes: one on the struct vector sub-field, named struct[sub].
        const string structVectorName = "clips[vector]";
        Console.WriteLine("Creating index...");
        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes =
            [
                new IndexParam(structVectorName, "struct_vec_idx", IndexType.Hnsw, MetricType.MaxSimCosine)
            ]
        });

        #region Snippet:MilvusStruct_Insert
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            RowsData =
            [
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["clips"] = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["int32"] = 1,
                            ["varchar"] = "clip-a",
                            ["vector"] = new[] { 0.1f, 0.2f, 0.3f, 0.4f }
                        },
                        new Dictionary<string, object?>
                        {
                            ["int32"] = 2,
                            ["varchar"] = "clip-b",
                            ["vector"] = new[] { 0.4f, 0.3f, 0.2f, 0.1f }
                        }
                    }
                },
                new Dictionary<string, object?>
                {
                    ["id"] = 2L,
                    ["clips"] = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["int32"] = 3,
                            ["varchar"] = "clip-c",
                            ["vector"] = new[] { 0.9f, 0.8f, 0.7f, 0.6f }
                        }
                    }
                }
            ]
        });
        #endregion

        Console.WriteLine("Loading collection...");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        // Search the struct vector sub-field: the embedding list carries the per-struct-element vectors of the
        // query, scored with MAX_SIM_COSINE (best element wins).
        var query = new EmbeddingList(
        [
            new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f, 0.3f, 0.4f }),
            new ReadOnlyMemory<float>(new[] { 0.4f, 0.3f, 0.2f, 0.1f })
        ]);

        Console.WriteLine("Searching...");
        SearchResp results = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = structVectorName,
            EmbeddingLists = new[] { query },
            MetricType = MetricType.MaxSimCosine,
            Limit = 5
        });
        Console.WriteLine($"Struct search returned {results.Ids.LongIds?.Count ?? 0} hits; top score = {results.Scores.FirstOrDefault():F4}");
        ExampleHelpers.PrintSearchHits(results);

        #region Snippet:MilvusStruct_AddStructField
        // A struct field can also be added to an existing collection; it must be nullable.
        var extraStructField = new StructFieldSchema("extra_struct", "additional metadata")
        {
            MaxCapacity = 8,
            Nullable = true
        };
        extraStructField.Fields.Add(new FieldSchema("rating", DataType.Int32));
        extraStructField.Fields.Add(FieldSchema.CreateVarchar("tag", maxLength: 128));
        await client.AddCollectionStructFieldAsync(new AddCollectionStructFieldReq
        {
            CollectionName = collectionName,
            StructField = extraStructField
        });
        #endregion

        Console.WriteLine("Adding struct field 'extra_struct'...");
        DescribeCollectionResp description = await client.DescribeCollectionAsync(
            new DescribeCollectionReq { CollectionName = collectionName });
        Console.WriteLine($"{description.Schema.StructFields.Count} struct fields in collection after add");

        // Existing rows carry null for the newly added struct field; query one to confirm.
        QueryResp existingRow = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id == 1",
            Parameters = new QueryParameters
            {
                OutputFields = { "id", "extra_struct" },
                ConsistencyLevel = ConsistencyLevel.Strong
            }
        });
        Console.WriteLine("Existing row after AddCollectionStructFieldAsync - new field is null");
        ExampleHelpers.PrintQueryRows(existingRow);

        // Insert a new row that carries values for the added struct field.
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            RowsData =
            [
                new Dictionary<string, object?>
                {
                    ["id"] = 3L,
                    ["clips"] = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["int32"] = 4,
                            ["varchar"] = "clip-d",
                            ["vector"] = new[] { 0.5f, 0.5f, 0.5f, 0.5f }
                        }
                    },
                    ["extra_struct"] = new object[]
                    {
                        new Dictionary<string, object?> { ["rating"] = 5, ["tag"] = "favorite" },
                        new Dictionary<string, object?> { ["rating"] = 4, ["tag"] = "classic" }
                    }
                }
            ]
        });

        // Query the new row, including the projected sub-fields of the added struct.
        QueryResp newRow = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id == 3",
            Parameters = new QueryParameters
            {
                OutputFields = { "id", "extra_struct", "extra_struct[rating]", "extra_struct[tag]" },
                ConsistencyLevel = ConsistencyLevel.Strong
            }
        });
        Console.WriteLine("New row with the added struct field");
        ExampleHelpers.PrintQueryRows(newRow);

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
