using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates nullable fields with default values. Mirrors cpp examples/src/v2/nullable_field.cpp
/// and java NullAndDefaultExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to declare a nullable column with a default value, then insert rows that
/// carry an explicit null (stored as NULL) and rows with a value.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c> (with <c>nullable</c>/<c>defaultValue</c>),
/// <c>InsertAsync</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> a successful insert (no exception) followed by "Done.".</para>
/// </remarks>
public static class NullableFieldExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
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
                    new FieldSchema("age", DataType.Int32) { Nullable = true, DefaultValue = 18 },
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }
            }
        });
        #endregion

        Console.WriteLine("Creating index...");
        await client.CreateIndexAsync(new Milvus.Client.V2.Requests.Index.CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [ new Milvus.Client.V2.Types.IndexParam("vector", null, IndexType.Flat, SimilarityMetricType.L2) ]
        });

        Console.WriteLine("Loading collection...");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        Console.WriteLine("Inserting...");
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            ColumnsData =
            [
                FieldData.Create("id", new long[] { 1, 2 }),
                FieldData.CreateVarChar("name", new[] { "alice", null! }),
                FieldData.Create("age", new int?[] { 30, null }),
                FieldData.CreateFloatVector("vector", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                    new ReadOnlyMemory<float>(new[] { 0f, 1f })
                })
            ]
        });

        Console.WriteLine("Querying rows where name is null...");
        QueryResp nullNameQuery = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "name is null",
            Parameters = new QueryParameters { OutputFields = { "id", "name", "age" } }
        });
        Console.WriteLine($"Query returned {nullNameQuery.RowCount} rows");
        ExampleHelpers.PrintQueryRows(nullNameQuery);

        Console.WriteLine("Searching with filter age is not null...");
        SearchResp ageSearch = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "vector",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 0f }) },
            MetricType = SimilarityMetricType.L2,
            Limit = 2,
            Parameters = new SearchParameters
            {
                Expression = "age is not null",
                OutputFields = { "id", "name", "age" }
            }
        });
        Console.WriteLine($"Search returned {ageSearch.Ids.LongIds?.Count ?? 0} hits");
        ExampleHelpers.PrintSearchHits(ageSearch);

        Console.WriteLine("Dropping collection...");
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
