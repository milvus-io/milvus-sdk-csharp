using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates a partition-key field. Mirrors cpp examples/src/v2/partition_key.cpp.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to declare a partition-key field and how a search with a filter on
/// that key routes to the matching partition.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c> (with <c>isPartitionKey</c>),
/// <c>CreateIndexAsync</c>, <c>InsertAsync</c>, <c>SearchAsync</c> with a partition-key filter,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Search returned 1 hits", then "Done.".</para>
/// </remarks>
public static class PartitionKeyExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "partition_key_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusPartitionKey_Schema
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    new FieldSchema("tenant", DataType.VarChar, isPartitionKey: true)
                    {
                        MaxLength = 64
                    },
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }
            }
        });
        #endregion

        
        await client.CreateIndexAsync(new Milvus.Client.V2.Requests.Index.CreateIndexReq
        {
            CollectionName = collectionName,
            FieldName = "vector",
            IndexType = IndexType.Flat,
            MetricType = SimilarityMetricType.L2
        });

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            Data =
            [
                FieldData.Create("id", new long[] { 1, 2 }),
                FieldData.CreateVarChar("tenant", new[] { "tenant_a", "tenant_b" }),
                FieldData.CreateFloatVector("vector", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                    new ReadOnlyMemory<float>(new[] { 0f, 1f })
                })
            ]
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        SearchResp results = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "vector",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 0f }) },
            MetricType = SimilarityMetricType.L2,
            Limit = 2,
            Parameters = new SearchParameters { Expression = "tenant == \"tenant_a\"" }
        });
        Console.WriteLine($"Search returned {results.Ids.LongIds?.Count ?? 0} hits");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
