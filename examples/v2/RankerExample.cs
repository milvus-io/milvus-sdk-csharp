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
/// Demonstrates function-score rerankers (boost and decay) applied to search results.
/// Mirrors cpp examples/src/v2/rerank_function.cpp and java RankerExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to re-rank dense-vector search hits with a
/// <see cref="BoostRerank" /> (boost rows matching a filter) or a <see cref="DecayRerank" />
/// (score falls with distance from an origin on a scalar field).</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>,
/// <c>InsertAsync</c>, <c>SearchAsync</c> with
/// <c>SearchParameters.FunctionScoreReranker</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> hit lists under "boost" and "decay", then "Done.".</para>
/// </remarks>
public static class RankerExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "ranker_example";
        const int dimension = 8;
        const int rowCount = 20;

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    new FieldSchema("year", DataType.Int32),
                    FieldSchema.CreateFloatVector("vector", dimension: dimension)
                }
            }
        });

        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [new IndexParam("vector", null, IndexType.Flat, SimilarityMetricType.Cosine)]
        });

        var rows = new List<IDictionary<string, object?>>();
        for (long i = 0; i < rowCount; i++)
        {
            rows.Add(new Dictionary<string, object?>
            {
                ["id"] = i,
                ["year"] = (int)(i % 20 + 2000),
                ["vector"] = new[] { i % 7 * 0.1f, i % 5 * 0.1f, i % 3 * 0.1f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f }
            });
        }

        await client.InsertAsync(new InsertReq { CollectionName = collectionName, RowsData = rows });
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        ReadOnlyMemory<float> query = new[] { 0.1f, 0.2f, 0.3f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };

        // Boost: rows whose "year" equals 2005 are pushed to the top.
        await SearchWithRerankAsync(client, collectionName, query,
            new BoostRerank("boost_fn")
                .WithFilter("year == 2005")
                .WithWeight(10f),
            "boost year == 2005");

        // Decay: relevance falls as "year" moves away from 2000 (gauss curve, scale 10).
        await SearchWithRerankAsync(client, collectionName, query,
            new DecayRerank("decay_fn")
                .WithInputField("year")
                .WithFunction("gauss")
                .WithOrigin(2000)
                .WithScale(10),
            "decay around year 2000");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }

    private static async Task SearchWithRerankAsync(
        MilvusClientV2 client, string collectionName, ReadOnlyMemory<float> query,
        IReranker reranker, string label)
    {
        SearchResp results = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "vector",
            Vectors = new[] { query },
            MetricType = SimilarityMetricType.Cosine,
            Limit = 5,
            Parameters = new SearchParameters
            {
                OutputFields = { "id", "year" },
                FunctionScoreReranker = reranker
            }
        });

        Console.WriteLine($"== {label} ==");
        SingleResult? single = results.SingleResults?.FirstOrDefault();
        if (single is not null)
        {
            for (int i = 0; i < (results.Ids.LongIds?.Count ?? 0); i++)
            {
                IReadOnlyDictionary<string, object?> row = single.GetRow(i);
                Console.WriteLine($"  id={row["id"]} year={row["year"]} score={results.Scores[i]:F4}");
            }
        }
    }
}
