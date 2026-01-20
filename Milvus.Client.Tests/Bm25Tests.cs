using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class Bm25Tests(MilvusFixture milvusFixture) : IDisposable
{
    private readonly MilvusClient Client = milvusFixture.CreateClient();

    [Fact]
    public async Task Bm25_full_text_search()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 5))
        {
            return;
        }

        MilvusCollection collection = Client.GetCollection(nameof(Bm25_full_text_search));
        await collection.DropAsync();

        var schema = new CollectionSchema
        {
            Fields =
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("text", maxLength: 1000, enableAnalyzer: true),
                FieldSchema.CreateSparseFloatVector("sparse_vector")
            },
            Functions =
            {
                FunctionSchema.CreateBm25("bm25_fn", "text", "sparse_vector")
            }
        };

        await Client.CreateCollectionAsync(collection.Name, schema);

        await collection.CreateIndexAsync(
            "sparse_vector",
            IndexType.SparseInvertedIndex,
            SimilarityMetricType.Bm25);

        await collection.InsertAsync(
        [
            FieldData.Create("id", new[] { 1L, 2L, 3L }),
            FieldData.CreateVarChar("text", new[]
            {
                "The quick brown fox jumps over the lazy dog",
                "A fast orange cat leaps across the sleepy hound",
                "Hello world, this is a test document"
            })
        ]);

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100),
            timeout: TimeSpan.FromMinutes(1));

        var results = await collection.SearchAsync(
            "sparse_vector",
            new[] { "quick fox" },
            limit: 3,
            new SearchParameters
            {
                ConsistencyLevel = ConsistencyLevel.Strong,
                OutputFields = { "text" }
            });

        Assert.NotNull(results.Ids.LongIds);
        Assert.True(results.Ids.LongIds.Count > 0);
        Assert.Equal(1L, results.Ids.LongIds[0]);

        var textField = (FieldData<string>)results.FieldsData.Single(f => f.FieldName == "text");
        Assert.Contains("quick", textField.Data[0]);
        Assert.Contains("fox", textField.Data[0]);
    }

    [Fact]
    public async Task Bm25_search_returns_correct_scores()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 5))
        {
            return;
        }

        MilvusCollection collection = Client.GetCollection(nameof(Bm25_search_returns_correct_scores));
        await collection.DropAsync();

        var schema = new CollectionSchema
        {
            Fields =
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("content", maxLength: 2000, enableAnalyzer: true),
                FieldSchema.CreateSparseFloatVector("bm25_vector")
            },
            Functions =
            {
                FunctionSchema.CreateBm25("content_bm25", "content", "bm25_vector")
            }
        };

        await Client.CreateCollectionAsync(collection.Name, schema);

        await collection.CreateIndexAsync(
            "bm25_vector",
            IndexType.SparseInvertedIndex,
            SimilarityMetricType.Bm25);

        await collection.InsertAsync(
        [
            FieldData.Create("id", new[] { 1L, 2L, 3L, 4L }),
            FieldData.CreateVarChar("content", new[]
            {
                "machine learning and artificial intelligence",
                "deep learning neural networks for image recognition",
                "natural language processing and text classification",
                "the weather today is sunny and warm"
            })
        ]);

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100),
            timeout: TimeSpan.FromMinutes(1));

        var results = await collection.SearchAsync(
            "bm25_vector",
            new[] { "learning" },
            limit: 4,
            new SearchParameters { ConsistencyLevel = ConsistencyLevel.Strong });

        Assert.NotNull(results.Ids.LongIds);
        Assert.True(results.Ids.LongIds.Count >= 2);
        Assert.True(results.Scores[0] > 0, "BM25 scores should be positive for matching documents");

        var topIds = results.Ids.LongIds.Take(2).ToList();
        Assert.Contains(1L, topIds);
        Assert.Contains(2L, topIds);
    }

    [Fact]
    public async Task Bm25_multiple_queries()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 5))
        {
            return;
        }

        MilvusCollection collection = Client.GetCollection(nameof(Bm25_multiple_queries));
        await collection.DropAsync();

        var schema = new CollectionSchema
        {
            Fields =
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("document", maxLength: 500, enableAnalyzer: true),
                FieldSchema.CreateSparseFloatVector("doc_vector")
            },
            Functions =
            {
                FunctionSchema.CreateBm25("doc_bm25", "document", "doc_vector")
            }
        };

        await Client.CreateCollectionAsync(collection.Name, schema);

        await collection.CreateIndexAsync(
            "doc_vector",
            IndexType.SparseInvertedIndex,
            SimilarityMetricType.Bm25);

        await collection.InsertAsync(
        [
            FieldData.Create("id", new[] { 1L, 2L, 3L }),
            FieldData.CreateVarChar("document", new[]
            {
                "apple banana orange fruit salad",
                "car truck motorcycle vehicle transportation",
                "computer laptop smartphone technology gadget"
            })
        ]);

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100),
            timeout: TimeSpan.FromMinutes(1));

        var results = await collection.SearchAsync(
            "doc_vector",
            new[] { "fruit", "vehicle" },
            limit: 2,
            new SearchParameters { ConsistencyLevel = ConsistencyLevel.Strong });

        Assert.NotNull(results.Ids.LongIds);
        Assert.Equal(2, results.NumQueries);
    }

    [Fact]
    public async Task Describe_collection_with_bm25_function()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 5))
        {
            return;
        }

        MilvusCollection collection = Client.GetCollection(nameof(Describe_collection_with_bm25_function));
        await collection.DropAsync();

        var schema = new CollectionSchema
        {
            Fields =
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("text_field", maxLength: 500, enableAnalyzer: true),
                FieldSchema.CreateSparseFloatVector("sparse_output")
            },
            Functions =
            {
                new FunctionSchema(
                    "my_bm25_function",
                    FunctionType.Bm25,
                    new[] { "text_field" },
                    new[] { "sparse_output" },
                    "BM25 function for full-text search")
            }
        };

        await Client.CreateCollectionAsync(collection.Name, schema);

        var description = await collection.DescribeAsync();

        Assert.Single(description.Schema.Functions);
        var function = description.Schema.Functions[0];
        Assert.Equal("my_bm25_function", function.Name);
        Assert.Equal(FunctionType.Bm25, function.Type);
        Assert.Single(function.InputFieldNames);
        Assert.Equal("text_field", function.InputFieldNames[0]);
        Assert.Single(function.OutputFieldNames);
        Assert.Equal("sparse_output", function.OutputFieldNames[0]);

        var fieldNames = string.Join(", ", description.Schema.Fields.Select(f => f.Name));
        var textField = description.Schema.Fields.SingleOrDefault(f => f.Name == "text_field");
        Assert.True(textField is not null, $"text_field not found. Available fields: {fieldNames}");
        Assert.True(textField.EnableAnalyzer, $"EnableAnalyzer should be true. Fields: {fieldNames}");

        var sparseField = description.Schema.Fields.SingleOrDefault(f => f.Name == "sparse_output");
        Assert.NotNull(sparseField);
        Assert.True(sparseField.IsFunctionOutput, "sparse_output should be marked as function output");
    }

    [Fact]
    public async Task Bm25_with_filter_expression()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 5))
        {
            return;
        }

        MilvusCollection collection = Client.GetCollection(nameof(Bm25_with_filter_expression));
        await collection.DropAsync();

        var schema = new CollectionSchema
        {
            Fields =
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.Create<long>("category"),
                FieldSchema.CreateVarchar("title", maxLength: 500, enableAnalyzer: true),
                FieldSchema.CreateSparseFloatVector("title_vector")
            },
            Functions =
            {
                FunctionSchema.CreateBm25("title_bm25", "title", "title_vector")
            }
        };

        await Client.CreateCollectionAsync(collection.Name, schema);

        await collection.CreateIndexAsync(
            "title_vector",
            IndexType.SparseInvertedIndex,
            SimilarityMetricType.Bm25);

        await collection.InsertAsync(
        [
            FieldData.Create("id", new[] { 1L, 2L, 3L, 4L }),
            FieldData.Create("category", new[] { 1L, 1L, 2L, 2L }),
            FieldData.CreateVarChar("title", new[]
            {
                "introduction to programming",
                "advanced programming techniques",
                "introduction to cooking",
                "advanced cooking recipes"
            })
        ]);

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100),
            timeout: TimeSpan.FromMinutes(1));

        var results = await collection.SearchAsync(
            "title_vector",
            new[] { "programming" },
            limit: 10,
            new SearchParameters
            {
                ConsistencyLevel = ConsistencyLevel.Strong,
                Expression = "category == 1",
                OutputFields = { "title", "category" }
            });

        Assert.NotNull(results.Ids.LongIds);
        Assert.Equal(2, results.Ids.LongIds.Count);

        var categoryField = (FieldData<long>)results.FieldsData.Single(f => f.FieldName == "category");
        Assert.All(categoryField.Data, c => Assert.Equal(1L, c));
    }

    [Fact]
    public async Task Bm25_hybrid_search_with_dense_vector()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 5))
        {
            return;
        }

        MilvusCollection collection = Client.GetCollection(nameof(Bm25_hybrid_search_with_dense_vector));
        await collection.DropAsync();

        var schema = new CollectionSchema
        {
            Fields =
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("title", maxLength: 500, enableAnalyzer: true),
                FieldSchema.CreateSparseFloatVector("title_sparse"),
                FieldSchema.CreateFloatVector("embedding", 4)
            },
            Functions =
            {
                FunctionSchema.CreateBm25("title_bm25", "title", "title_sparse")
            }
        };

        await Client.CreateCollectionAsync(collection.Name, schema);

        await collection.CreateIndexAsync("title_sparse", IndexType.SparseInvertedIndex, SimilarityMetricType.Bm25);
        await collection.CreateIndexAsync("embedding", IndexType.Flat, SimilarityMetricType.L2);

        await collection.InsertAsync(
        [
            FieldData.Create("id", new[] { 1L, 2L, 3L, 4L }),
            FieldData.CreateVarChar("title", new[]
            {
                "machine learning algorithms",
                "deep learning neural networks",
                "cooking recipes for beginners",
                "machine learning for cooking automation"
            }),
            FieldData.CreateFloatVector("embedding", new ReadOnlyMemory<float>[]
            {
                new[] { 1.0f, 0.0f, 0.0f, 0.0f },
                new[] { 0.9f, 0.1f, 0.0f, 0.0f },
                new[] { 0.0f, 0.0f, 1.0f, 0.0f },
                new[] { 0.5f, 0.0f, 0.5f, 0.0f }
            })
        ]);

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100),
            timeout: TimeSpan.FromMinutes(1));

        var results = await collection.HybridSearchAsync(
            [
                new TextAnnSearchRequest("title_sparse", ["machine learning"], limit: 4),
                new VectorAnnSearchRequest<float>(
                    "embedding",
                    [new[] { 1.0f, 0.0f, 0.0f, 0.0f }],
                    SimilarityMetricType.L2,
                    limit: 4)
            ],
            new RrfReranker(),
            limit: 4,
            new HybridSearchParameters
            {
                ConsistencyLevel = ConsistencyLevel.Strong,
                OutputFields = { "title" }
            });

        Assert.NotNull(results.Ids.LongIds);
        Assert.True(results.Ids.LongIds.Count > 0);

        Assert.Equal(1L, results.Ids.LongIds[0]);

        var titleField = (FieldData<string>)results.FieldsData.Single(f => f.FieldName == "title");
        Assert.Contains("machine learning", titleField.Data[0]);
    }

    [Fact]
    public async Task Bm25_with_custom_index_parameters()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 5))
        {
            return;
        }

        MilvusCollection collection = Client.GetCollection(nameof(Bm25_with_custom_index_parameters));
        await collection.DropAsync();

        var schema = new CollectionSchema
        {
            Fields =
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("text", maxLength: 1000, enableAnalyzer: true),
                FieldSchema.CreateSparseFloatVector("sparse_vector")
            },
            Functions =
            {
                FunctionSchema.CreateBm25("bm25_fn", "text", "sparse_vector")
            }
        };

        await Client.CreateCollectionAsync(collection.Name, schema);

        await collection.CreateIndexAsync(
            "sparse_vector",
            IndexType.SparseInvertedIndex,
            SimilarityMetricType.Bm25,
            extraParams: new Dictionary<string, string>
            {
                ["inverted_index_algo"] = "\"DAAT_WAND\"",
                ["bm25_k1"] = "1.5",
                ["bm25_b"] = "0.75"
            });

        await collection.InsertAsync(
        [
            FieldData.Create("id", new[] { 1L, 2L, 3L }),
            FieldData.CreateVarChar("text", new[]
            {
                "The quick brown fox jumps over the lazy dog",
                "A fast orange cat leaps across the sleepy hound",
                "Hello world, this is a test document"
            })
        ]);

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100),
            timeout: TimeSpan.FromMinutes(1));

        var results = await collection.SearchAsync(
            "sparse_vector",
            new[] { "quick fox" },
            limit: 3,
            new SearchParameters
            {
                ConsistencyLevel = ConsistencyLevel.Strong,
                OutputFields = { "text" }
            });

        Assert.NotNull(results.Ids.LongIds);
        Assert.True(results.Ids.LongIds.Count > 0);
        Assert.Equal(1L, results.Ids.LongIds[0]);
    }

    [Fact]
    public async Task Bm25_hybrid_search_with_weighted_reranker()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 5))
        {
            return;
        }

        MilvusCollection collection = Client.GetCollection(nameof(Bm25_hybrid_search_with_weighted_reranker));
        await collection.DropAsync();

        var schema = new CollectionSchema
        {
            Fields =
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("content", maxLength: 1000, enableAnalyzer: true),
                FieldSchema.CreateSparseFloatVector("content_sparse"),
                FieldSchema.CreateFloatVector("content_embedding", 2)
            },
            Functions =
            {
                FunctionSchema.CreateBm25("content_bm25", "content", "content_sparse")
            }
        };

        await Client.CreateCollectionAsync(collection.Name, schema);

        await collection.CreateIndexAsync("content_sparse", IndexType.SparseInvertedIndex, SimilarityMetricType.Bm25);
        await collection.CreateIndexAsync("content_embedding", IndexType.Flat, SimilarityMetricType.L2);

        await collection.InsertAsync(
        [
            FieldData.Create("id", new[] { 1L, 2L, 3L }),
            FieldData.CreateVarChar("content", new[]
            {
                "python programming language tutorial",
                "java enterprise application development",
                "python web framework django flask"
            }),
            FieldData.CreateFloatVector("content_embedding", new ReadOnlyMemory<float>[]
            {
                new[] { 1.0f, 0.0f },
                new[] { 0.0f, 1.0f },
                new[] { 0.8f, 0.2f }
            })
        ]);

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100),
            timeout: TimeSpan.FromMinutes(1));

        var results = await collection.HybridSearchAsync(
            [
                new TextAnnSearchRequest("content_sparse", ["python"], limit: 3),
                new VectorAnnSearchRequest<float>(
                    "content_embedding",
                    [new[] { 1.0f, 0.0f }],
                    SimilarityMetricType.L2,
                    limit: 3)
            ],
            new WeightedReranker(0.7f, 0.3f),
            limit: 3,
            new HybridSearchParameters { ConsistencyLevel = ConsistencyLevel.Strong });

        Assert.NotNull(results.Ids.LongIds);
        Assert.True(results.Ids.LongIds.Count > 0);

        var topIds = results.Ids.LongIds.Take(2).ToHashSet();
        Assert.Contains(1L, topIds);
    }

    public void Dispose() => Client.Dispose();
}
