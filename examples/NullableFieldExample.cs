using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates nullable fields with default values. Mirrors cpp examples/src/v2/nullable_field.cpp
/// and java NullAndDefaultExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to declare a nullable column with a default value, then insert rows
/// that omit the value (server fills the default).</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c> (with <c>nullable</c>/<c>defaultValue</c>),
/// <c>InsertAsync</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> a successful insert (no exception) followed by "Done.".</para>
/// </remarks>
public static class NullableFieldExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "nullable_field_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusNullableField_Schema
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar("name", maxLength: 64, nullable: true, defaultValue: "anonymous"),
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }
            }
        });
        #endregion

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            Data =
            [
                FieldData.Create("id", new long[] { 1, 2 }),
                FieldData.CreateVarChar("name", new[] { "alice", null! }),
                FieldData.CreateFloatVector("vector", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                    new ReadOnlyMemory<float>(new[] { 0f, 1f })
                })
            ]
        });

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
