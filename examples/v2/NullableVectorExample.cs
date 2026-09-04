using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates nullable vector fields: a vector field declared nullable accepts null rows,
/// and a nullable vector field can be added to an existing collection.
/// Mirrors cpp examples/src/v2/nullable_vector.cpp and java NullableVectorExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show null handling for vector fields — rows that supply no vector are
/// stored as null (valid_data=false) and come back as null.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c> (nullable vector field),
/// <c>InsertAsync</c>, <c>QueryAsync</c>, <c>AddCollectionFieldAsync</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "row 0 vector is null: False" (row 0 was inserted with a vector),
/// "row 1 vector is null: True" (row 1 was inserted without one), then "Done.".</para>
/// </remarks>
public static class NullableVectorExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "nullable_vector_example";

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        // A vector field with Nullable=true accepts null rows.
        var vectorField = FieldSchema.CreateFloatVector("vector", dimension: 4);
        vectorField.Nullable = true;

        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    vectorField
                }
            }
        });

        #region Snippet:MilvusNullableVector_Insert
        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [new IndexParam("vector", null, IndexType.Flat, SimilarityMetricType.L2)]
        });

        // Row 1 supplies a vector; row 2 supplies null for the vector field.
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            RowsData =
            [
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["vector"] = new[] { 0.1f, 0.2f, 0.3f, 0.4f }
                },
                new Dictionary<string, object?>
                {
                    ["id"] = 2L,
                    ["vector"] = null
                }
            ]
        });
        #endregion

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        QueryResp query = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id in [1, 2]",
            Parameters = new QueryParameters { OutputFields = { "id", "vector" } }
        });

        FloatVectorFieldData vecField = AssertVectorField(query.FieldsData);
        Console.WriteLine($"row 0 vector is null: {vecField.Data[0].IsEmpty}");
        Console.WriteLine($"row 1 vector is null: {vecField.Data[1].IsEmpty}");

        // A nullable vector field can also be added to an existing collection.
        var added = FieldSchema.CreateFloatVector("extra_vector", dimension: 2);
        added.Nullable = true;
        await client.AddCollectionFieldAsync(new AddCollectionFieldReq { CollectionName = collectionName, Field = added });

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }

    private static FloatVectorFieldData AssertVectorField(IReadOnlyList<FieldData> fields)
        => (FloatVectorFieldData)fields.First(f => f.FieldName == "vector");
}
