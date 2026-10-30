using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates the server-side search iterator: page over the top-K nearest neighbours of a
/// search in <c>BatchSize</c>-sized batches via <c>await foreach</c>, including a client-side
/// page filter (<c>ExternalFilterFunc</c>) that prunes hits before they are yielded.
/// Mirrors cpp examples/src/v2/iterator_search.cpp.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to consume a large search result set without loading it at
/// once. The search iterator is top-K ordered and yields one <c>SingleResult</c> per page.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>,
/// <c>InsertAsync</c>, <c>SearchIteratorAsync</c>, <c>SearchIteratorReq.ExternalFilterFunc</c>,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> a hit count per search-iterator page, then "Done.".</para>
/// </remarks>
public static class IteratorSearchExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        const string collectionName = "iterator_search_example";
        const string ageField = "age";
        const string vectorField = "vector";
        const int dimension = 4;
        const int rowCount = 100;

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        Console.WriteLine("Creating collection...");
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    new FieldSchema(ageField, DataType.Int8),
                    FieldSchema.CreateFloatVector(vectorField, dimension: dimension)
                }
            }
        });

        Console.WriteLine("Creating index...");
        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [new IndexParam(vectorField, null, IndexType.Flat, MetricType.L2)]
        });

        var vectors = Enumerable.Range(0, rowCount)
            .Select(i => new ReadOnlyMemory<float>(new[] { i % 10 / 10f, i % 7 / 10f, i % 5 / 10f, i % 3 / 10f }))
            .ToArray();

        Console.WriteLine("Inserting 100 rows...");
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            ColumnsData =
            [
                FieldData.Create("id", Enumerable.Range(0, rowCount).Select(i => (long)i).ToArray()),
                FieldData.Create(ageField, Enumerable.Range(0, rowCount).Select(i => (sbyte)(i % 10)).ToArray()),
                FieldData.CreateFloatVector(vectorField, vectors)
            ]
        });

        Console.WriteLine("Loading collection...");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        #region Snippet:MilvusIteratorSearch_Search
        Console.WriteLine("Search iterator (batch size 10):");
        int searchHits = 0;
        await foreach (SingleResult batch in client.SearchIteratorAsync(
            new SearchIteratorReq
            {
                CollectionName = collectionName,
                VectorFieldName = vectorField,
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.5f, 0.5f, 0.5f, 0.5f }) },
                MetricType = MetricType.L2,
                BatchSize = 10,
                Limit = 50
            }))
        {
            Console.WriteLine($"  page of {batch.Scores.Count} hits");
            searchHits += batch.Scores.Count;
        }

        Console.WriteLine($"  total hits iterated: {searchHits}");
        #endregion

        #region Snippet:MilvusIteratorSearch_ExternalFilter
        Console.WriteLine("Search iterator with client-side page filter (keep even ages only):");
        int filteredHits = 0;
        await foreach (SingleResult batch in client.SearchIteratorAsync(
            new SearchIteratorReq
            {
                CollectionName = collectionName,
                VectorFieldName = vectorField,
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.5f, 0.5f, 0.5f, 0.5f }) },
                MetricType = MetricType.L2,
                BatchSize = 10,
                Limit = 50,
                Parameters = new SearchParameters { OutputFields = { "id", ageField } },
                // The callback receives each fetched page and returns the 0-based row indices to keep.
                // Here we drop every hit whose "age" output field is odd.
                ExternalFilterFunc = page =>
                {
                    FieldData? ageFieldData = page.OutputFields.FirstOrDefault(f => f.FieldName == ageField);
                    if (ageFieldData is null)
                    {
                        throw new ArgumentException("age output field missing in search result");
                    }

                    var ages = (FieldData<sbyte>)ageFieldData;
                    return Enumerable.Range(0, ages.Data.Count)
                        .Where(i => ages.Data[i] % 2 == 0)
                        .ToList();
                }
            }))
        {
            Console.WriteLine($"  page of {batch.Scores.Count} hits");
            foreach (int i in Enumerable.Range(0, batch.Scores.Count))
            {
                if (((sbyte)batch.GetRow(i)[ageField]!) % 2 != 0)
                {
                    Console.WriteLine("  ERROR: a row with odd age slipped through the external filter");
                }
            }

            filteredHits += batch.Scores.Count;
        }

        Console.WriteLine($"  total hits iterated with external filter: {filteredHits}");
        #endregion

        Console.WriteLine("Deleting...");
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
