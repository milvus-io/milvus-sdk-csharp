using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// A broader tour of the SDK: schema with several field types, insert, query, search,
/// upsert, delete. Mirrors cpp examples/src/v2/general.cpp and java GeneralExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> exercise the core DML/DQL surface on a single collection, showing how
/// field types map to <c>FieldData</c> values.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>,
/// <c>InsertAsync</c>, <c>QueryAsync</c>, <c>SearchAsync</c>, <c>UpsertAsync</c>,
/// <c>DeleteAsync</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Query age &gt; 25 returned 2 rows",
/// "Search returned 2 hits", "Upsert count: 2", then "Done.".</para>
/// </remarks>
public static class GeneralExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "general_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusGeneral_Schema
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar("name", maxLength: 128),
                    new FieldSchema("age", DataType.Int32),
                    FieldSchema.CreateFloatVector("vector", dimension: 4)
                }
            }
        });

        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            FieldName = "vector",
            IndexType = IndexType.Flat,
            MetricType = SimilarityMetricType.L2
        });
        #endregion

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            Data =
            [
                FieldData.Create("id", new long[] { 1, 2, 3 }),
                FieldData.CreateVarChar("name", new[] { "alice", "bob", "carol" }),
                FieldData.Create("age", new[] { 30, 25, 35 }),
                FieldData.CreateFloatVector("vector", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 1f, 0f, 0f, 0f }),
                    new ReadOnlyMemory<float>(new[] { 0f, 1f, 0f, 0f }),
                    new ReadOnlyMemory<float>(new[] { 0f, 0f, 1f, 0f })
                })
            ]
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        QueryResp query = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "age > 25",
            Parameters = new QueryParameters { OutputFields = { "id", "name", "age" } }
        });
        Console.WriteLine($"Query age > 25 returned {query.FieldsData.FirstOrDefault()?.RowCount ?? 0} rows");

        SearchResp search = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "vector",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 0f, 0f, 0f }) },
            MetricType = SimilarityMetricType.L2,
            Limit = 2,
            Parameters = new SearchParameters { OutputFields = { "name" } }
        });
        Console.WriteLine($"Search returned {search.Ids.LongIds?.Count ?? 0} hits");

        // Upsert one existing and one new row.
        await client.UpsertAsync(new UpsertReq
        {
            CollectionName = collectionName,
            Data =
            [
                FieldData.Create("id", new long[] { 2, 4 }),
                FieldData.CreateVarChar("name", new[] { "bob2", "dave" }),
                FieldData.Create("age", new[] { 26, 40 }),
                FieldData.CreateFloatVector("vector", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 0f, 1f, 0f, 0f }),
                    new ReadOnlyMemory<float>(new[] { 0f, 0f, 0f, 1f })
                })
            ]
        });

        await client.DeleteAsync(new DeleteReq
        {
            CollectionName = collectionName,
            Expression = "id in [1]"
        });

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
