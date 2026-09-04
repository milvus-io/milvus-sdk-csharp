using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// The simplest possible example: connect, create a collection, insert, search, drop.
/// Mirrors cpp examples/src/v2/simple.cpp and java SimpleExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> end-to-end quickstart — the smallest program that puts data in Milvus and
/// reads it back.</para>
/// <para><b>APIs used:</b> <c>ConnectAsync</c>, <c>CreateCollectionAsync</c>,
/// <c>CreateIndexAsync</c>, <c>InsertAsync</c>, <c>LoadCollectionAsync</c>, <c>SearchAsync</c>,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> a "Connected to …" line, then
/// "Search returned 3 hits; top score = 0.0000" and a final "Done.".</para>
/// </remarks>
public static class SimpleExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);

        #region Snippet:MilvusSimple_Connect
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();
        Console.WriteLine($"Connected to {uri}");
        #endregion

        const string collectionName = "hello_milvus";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        Console.WriteLine("Creating collection...");
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("pk", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar("random", maxLength: 100),
                    FieldSchema.CreateFloatVector("embeddings", dimension: 8)
                }
            }
        });

        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            FieldName = "embeddings",
            IndexType = IndexType.AutoIndex,
            MetricType = SimilarityMetricType.Cosine
        });

        Console.WriteLine("Inserting 10 rows...");
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            Data =
            [
                FieldData.Create("pk", Enumerable.Range(0, 10).Select(i => (long)i).ToArray()),
                FieldData.CreateVarChar("random", Enumerable.Range(0, 10).Select(i => $"rand_{Guid.NewGuid():N}").ToArray()),
                FieldData.CreateFloatVector("embeddings", Enumerable.Range(0, 10)
                    .Select(i => new ReadOnlyMemory<float>(Enumerable.Repeat((float)i, 8).ToArray())).ToArray())
            ]
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        Console.WriteLine("Searching...");
        SearchResp results = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "embeddings",
            Vectors = new[] { new ReadOnlyMemory<float>(new float[8]) },
            MetricType = SimilarityMetricType.Cosine,
            Limit = 3,
            Parameters = new SearchParameters { OutputFields = { "pk" } }
        });

        Console.WriteLine($"Search returned {results.Ids.LongIds?.Count ?? 0} hits; top score = {results.Scores.FirstOrDefault():F4}");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
