using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
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
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Struct search returned N hits", then "Done.".</para>
/// </remarks>
public static class StructExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
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
        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes =
            [
                new IndexParam(structVectorName, "struct_vec_idx", IndexType.Hnsw, SimilarityMetricType.MaxSimCosine)
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

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        // Search the struct vector sub-field: the embedding list carries the per-struct-element vectors of the
        // query, scored with MAX_SIM_COSINE (best element wins).
        var query = new EmbeddingList(
        [
            new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f, 0.3f, 0.4f }),
            new ReadOnlyMemory<float>(new[] { 0.4f, 0.3f, 0.2f, 0.1f })
        ]);

        SearchResp results = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = structVectorName,
            EmbeddingLists = new[] { query },
            MetricType = SimilarityMetricType.MaxSimCosine,
            Limit = 5
        });
        Console.WriteLine($"Struct search returned {results.Ids.LongIds?.Count ?? 0} hits; top score = {results.Scores.FirstOrDefault():F4}");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
