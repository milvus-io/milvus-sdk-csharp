using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class HybridSearchTests(
    MilvusFixture milvusFixture,
    HybridSearchTests.HybridSearchCollectionFixture hybridSearchCollectionFixture)
    : IClassFixture<HybridSearchTests.HybridSearchCollectionFixture>, IDisposable
{
    private readonly MilvusClient Client = milvusFixture.CreateClient();
    private MilvusCollection Collection => hybridSearchCollectionFixture.Collection;
    private string CollectionName => Collection.Name;
    private bool SupportsHybridSearch => hybridSearchCollectionFixture.SupportsHybridSearch;

    [Fact]
    public async Task HybridSearch_with_RRF_reranker()
    {
        if (!SupportsHybridSearch)
        {
            return;
        }

        var results = await Collection.HybridSearchAsync(
            [
                new VectorAnnSearchRequest<float>(
                    "float_vector_1",
                    [new[] { 1f, 2f }],
                    SimilarityMetricType.L2,
                    limit: 3),
                new VectorAnnSearchRequest<float>(
                    "float_vector_2",
                    [new[] { 0.1f, 0.2f }],
                    SimilarityMetricType.L2,
                    limit: 3)
            ],
            new RrfReranker(),
            limit: 3,
            new HybridSearchParameters { ConsistencyLevel = ConsistencyLevel.Strong });

        Assert.Equal(CollectionName, results.CollectionName);
        Assert.NotNull(results.Ids.LongIds);
        Assert.Equal(3, results.Ids.LongIds.Count);
        Assert.Equal(1L, results.Ids.LongIds[0]);
    }

    [Fact]
    public async Task HybridSearch_with_Rrf_reranker_custom_k()
    {
        if (!SupportsHybridSearch)
        {
            return;
        }

        var results = await Collection.HybridSearchAsync(
            [
                new VectorAnnSearchRequest<float>(
                    "float_vector_1",
                    [new[] { 1f, 2f }],
                    SimilarityMetricType.L2,
                    limit: 3),
                new VectorAnnSearchRequest<float>(
                    "float_vector_2",
                    [new[] { 0.1f, 0.2f }],
                    SimilarityMetricType.L2,
                    limit: 3)
            ],
            new RrfReranker(k: 100),
            limit: 3,
            new HybridSearchParameters { ConsistencyLevel = ConsistencyLevel.Strong });

        Assert.Equal(CollectionName, results.CollectionName);
        Assert.NotNull(results.Ids.LongIds);
        Assert.Equal(3, results.Ids.LongIds.Count);
        Assert.Equal(1L, results.Ids.LongIds[0]);
    }

    [Fact]
    public async Task HybridSearch_with_weighted_reranker()
    {
        if (!SupportsHybridSearch)
        {
            return;
        }

        var results = await Collection.HybridSearchAsync(
            [
                new VectorAnnSearchRequest<float>(
                    "float_vector_1",
                    [new[] { 1f, 2f }],
                    SimilarityMetricType.L2,
                    limit: 3),
                new VectorAnnSearchRequest<float>(
                    "float_vector_2",
                    [new[] { 0.1f, 0.2f }],
                    SimilarityMetricType.L2,
                    limit: 3)
            ],
            new WeightedReranker(0.7f, 0.3f),
            limit: 3,
            new HybridSearchParameters { ConsistencyLevel = ConsistencyLevel.Strong });

        Assert.Equal(CollectionName, results.CollectionName);
        Assert.NotNull(results.Ids.LongIds);
        Assert.Equal(3, results.Ids.LongIds.Count);
        Assert.Equal(1L, results.Ids.LongIds[0]);
    }

    [Fact]
    public async Task HybridSearch_with_output_fields()
    {
        if (!SupportsHybridSearch)
        {
            return;
        }

        var results = await Collection.HybridSearchAsync(
            [
                new VectorAnnSearchRequest<float>(
                    "float_vector_1",
                    [new[] { 1f, 2f }],
                    SimilarityMetricType.L2,
                    limit: 3),
                new VectorAnnSearchRequest<float>(
                    "float_vector_2",
                    [new[] { 0.1f, 0.2f }],
                    SimilarityMetricType.L2,
                    limit: 3)
            ],
            new RrfReranker(),
            limit: 3,
            new HybridSearchParameters
            {
                OutputFields = { "id", "varchar" },
                ConsistencyLevel = ConsistencyLevel.Strong
            });

        Assert.Equal(CollectionName, results.CollectionName);
        Assert.NotNull(results.Ids.LongIds);
        Assert.Equal(3, results.Ids.LongIds.Count);

        var idField = results.FieldsData.FirstOrDefault(f => f.FieldName == "id");
        Assert.NotNull(idField);
        Assert.Equal([1L, 2L, 3L], ((FieldData<long>)idField).Data);

        var varcharField = results.FieldsData.FirstOrDefault(f => f.FieldName == "varchar");
        Assert.NotNull(varcharField);
        Assert.Equal(["one", "two", "three"], ((FieldData<string>)varcharField).Data);
    }

    [Fact]
    public async Task HybridSearch_with_expression_filter()
    {
        if (!SupportsHybridSearch)
        {
            return;
        }

        var request1 = new VectorAnnSearchRequest<float>(
            "float_vector_1",
            [new[] { 1f, 2f }],
            SimilarityMetricType.L2,
            limit: 5)
        {
            Expression = "id > 2"
        };

        var request2 = new VectorAnnSearchRequest<float>(
            "float_vector_2",
            [new[] { 0.1f, 0.2f }],
            SimilarityMetricType.L2,
            limit: 5)
        {
            Expression = "id > 2"
        };

        var results = await Collection.HybridSearchAsync(
            [request1, request2],
            new RrfReranker(),
            limit: 3,
            new HybridSearchParameters
            {
                ConsistencyLevel = ConsistencyLevel.Strong
            });

        Assert.Equal(CollectionName, results.CollectionName);
        Assert.NotNull(results.Ids.LongIds);
        Assert.Equal(3, results.Ids.LongIds.Count);
        Assert.All(results.Ids.LongIds, id => Assert.True(id > 2));
        Assert.Equal(3L, results.Ids.LongIds[0]);
    }

    [Fact]
    public async Task HybridSearch_with_group_by()
    {
        if (!SupportsHybridSearch)
        {
            return;
        }

        var results = await Collection.HybridSearchAsync(
            [
                new VectorAnnSearchRequest<float>(
                    "float_vector_1",
                    [new[] { 1f, 2f }],
                    SimilarityMetricType.L2,
                    limit: 5),
                new VectorAnnSearchRequest<float>(
                    "float_vector_2",
                    [new[] { 0.1f, 0.2f }],
                    SimilarityMetricType.L2,
                    limit: 5)
            ],
            new RrfReranker(),
            limit: 3,
            new HybridSearchParameters
            {
                GroupByField = "id",
                ConsistencyLevel = ConsistencyLevel.Strong
            });

        Assert.Equal(CollectionName, results.CollectionName);
        Assert.NotNull(results.Ids.LongIds);
        Assert.Equal(3, results.Ids.LongIds.Count);
        Assert.Equal(1L, results.Ids.LongIds[0]);
    }

    [Fact]
    public async Task HybridSearch_with_group_size()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 6))
        {
            return;
        }

        MilvusCollection collection = Client.GetCollection(nameof(HybridSearch_with_group_size));
        await collection.DropAsync();
        await Client.CreateCollectionAsync(
            collection.Name,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.Create<long>("group_id"),
                FieldSchema.CreateFloatVector("float_vector_1", 2),
                FieldSchema.CreateFloatVector("float_vector_2", 2)
            });

        await collection.CreateIndexAsync("float_vector_1", IndexType.Flat, SimilarityMetricType.L2);
        await collection.CreateIndexAsync("float_vector_2", IndexType.Flat, SimilarityMetricType.L2);

        await collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new[] { 1L, 2L, 3L, 4L, 5L, 6L }),
                FieldData.Create("group_id", new[] { 1L, 1L, 1L, 2L, 2L, 2L }),
                FieldData.CreateFloatVector("float_vector_1", new ReadOnlyMemory<float>[]
                {
                    new[] { 1f, 2f },
                    new[] { 1.1f, 2.1f },
                    new[] { 1.2f, 2.2f },
                    new[] { 10f, 20f },
                    new[] { 10.1f, 20.1f },
                    new[] { 10.2f, 20.2f }
                }),
                FieldData.CreateFloatVector("float_vector_2", new ReadOnlyMemory<float>[]
                {
                    new[] { 0.1f, 0.2f },
                    new[] { 0.11f, 0.21f },
                    new[] { 0.12f, 0.22f },
                    new[] { 1f, 2f },
                    new[] { 1.1f, 2.1f },
                    new[] { 1.2f, 2.2f }
                })
            });

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        var results = await collection.HybridSearchAsync(
            [
                new VectorAnnSearchRequest<float>(
                    "float_vector_1",
                    [new[] { 1f, 2f }],
                    SimilarityMetricType.L2,
                    limit: 5),
                new VectorAnnSearchRequest<float>(
                    "float_vector_2",
                    [new[] { 0.1f, 0.2f }],
                    SimilarityMetricType.L2,
                    limit: 5)
            ],
            new RrfReranker(),
            limit: 2,
            new HybridSearchParameters
            {
                GroupByField = "group_id",
                GroupSize = 2,
                OutputFields = { "group_id" },
                ConsistencyLevel = ConsistencyLevel.Strong
            });

        var groupIdField = (FieldData<long>)results.FieldsData.Single(f => f.FieldName == "group_id");
        Assert.True(groupIdField.Data.Count(g => g == 1L) >= 1);
        Assert.True(groupIdField.Data.Count(g => g == 2L) >= 1);
        Assert.True(results.Ids.LongIds!.Count > 2);
    }

    [Fact]
    public async Task HybridSearch_with_strict_group_size()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 6))
        {
            return;
        }

        MilvusCollection collection = Client.GetCollection(nameof(HybridSearch_with_strict_group_size));
        await collection.DropAsync();
        await Client.CreateCollectionAsync(
            collection.Name,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.Create<long>("group_id"),
                FieldSchema.CreateFloatVector("float_vector_1", 2),
                FieldSchema.CreateFloatVector("float_vector_2", 2)
            });

        await collection.CreateIndexAsync("float_vector_1", IndexType.Flat, SimilarityMetricType.L2);
        await collection.CreateIndexAsync("float_vector_2", IndexType.Flat, SimilarityMetricType.L2);

        await collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("id", new[] { 1L, 2L, 3L, 4L }),
                FieldData.Create("group_id", new[] { 1L, 1L, 1L, 2L }),
                FieldData.CreateFloatVector("float_vector_1", new ReadOnlyMemory<float>[]
                {
                    new[] { 1f, 2f },
                    new[] { 1.1f, 2.1f },
                    new[] { 1.2f, 2.2f },
                    new[] { 10f, 20f }
                }),
                FieldData.CreateFloatVector("float_vector_2", new ReadOnlyMemory<float>[]
                {
                    new[] { 0.1f, 0.2f },
                    new[] { 0.11f, 0.21f },
                    new[] { 0.12f, 0.22f },
                    new[] { 1f, 2f }
                })
            });

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        var results = await collection.HybridSearchAsync(
            [
                new VectorAnnSearchRequest<float>(
                    "float_vector_1",
                    [new[] { 1f, 2f }],
                    SimilarityMetricType.L2,
                    limit: 5),
                new VectorAnnSearchRequest<float>(
                    "float_vector_2",
                    [new[] { 0.1f, 0.2f }],
                    SimilarityMetricType.L2,
                    limit: 5)
            ],
            new RrfReranker(),
            limit: 2,
            new HybridSearchParameters
            {
                GroupByField = "group_id",
                GroupSize = 2,
                StrictGroupSize = true,
                OutputFields = { "group_id" },
                ConsistencyLevel = ConsistencyLevel.Strong
            });

        var groupIdField = (FieldData<long>)results.FieldsData.Single(f => f.FieldName == "group_id");
        Assert.Equal(2, groupIdField.Data.Count(g => g == 1L));
    }

    [Fact]
    public async Task HybridSearch_with_partition_names()
    {
        if (!SupportsHybridSearch)
        {
            return;
        }

        var results = await Collection.HybridSearchAsync(
            [
                new VectorAnnSearchRequest<float>(
                    "float_vector_1",
                    [new[] { 1f, 2f }],
                    SimilarityMetricType.L2,
                    limit: 3),
                new VectorAnnSearchRequest<float>(
                    "float_vector_2",
                    [new[] { 0.1f, 0.2f }],
                    SimilarityMetricType.L2,
                    limit: 3)
            ],
            new RrfReranker(),
            limit: 3,
            new HybridSearchParameters
            {
                PartitionNames = { "_default" },
                ConsistencyLevel = ConsistencyLevel.Strong
            });

        Assert.Equal(CollectionName, results.CollectionName);
        Assert.NotNull(results.Ids.LongIds);
        Assert.Equal(3, results.Ids.LongIds.Count);
        Assert.Equal(1L, results.Ids.LongIds[0]);
    }

    [Fact]
    public async Task HybridSearch_with_multiple_query_vectors()
    {
        if (!SupportsHybridSearch)
        {
            return;
        }

        var results = await Collection.HybridSearchAsync(
            [
                new VectorAnnSearchRequest<float>(
                    "float_vector_1",
                    [
                        new[] { 1f, 2f },
                        new[] { 5f, 6f }
                    ],
                    SimilarityMetricType.L2,
                    limit: 3),
                new VectorAnnSearchRequest<float>(
                    "float_vector_2",
                    [
                        new[] { 0.1f, 0.2f },
                        new[] { 0.5f, 0.6f }
                    ],
                    SimilarityMetricType.L2,
                    limit: 3)
            ],
            new RrfReranker(),
            limit: 3,
            new HybridSearchParameters { ConsistencyLevel = ConsistencyLevel.Strong });

        Assert.Equal(CollectionName, results.CollectionName);
        Assert.NotNull(results.Ids.LongIds);
        Assert.True(results.Ids.LongIds.Count > 0);
    }

    [Fact]
    public async Task HybridSearch_sparse_vectors()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        var collection = Client.GetCollection(nameof(HybridSearch_sparse_vectors));
        await collection.DropAsync();

        await Client.CreateCollectionAsync(
            collection.Name,
            [
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2),
                FieldSchema.CreateSparseFloatVector("sparse_vector")
            ]);

        var sparseVectors = new[]
        {
            new MilvusSparseVector<float>((int[])[0, 1], (float[])[1.0f, 2.0f]),
            new MilvusSparseVector<float>((int[])[0, 1], (float[])[10.0f, 20.0f]),
            new MilvusSparseVector<float>((int[])[2, 3], (float[])[5.0f, 6.0f]),
        };

        await collection.InsertAsync([
            FieldData.Create("id", [1L, 2L, 3L]),
            FieldData.CreateFloatVector("float_vector", [
                new[] { 1f, 2f },
                new[] { 3f, 4f },
                new[] { 5f, 6f }
            ]),
            FieldData.CreateSparseFloatVector("sparse_vector", sparseVectors)
        ]);

        await collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);
        await collection.CreateIndexAsync("sparse_vector", IndexType.SparseInvertedIndex, SimilarityMetricType.Ip);

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100),
            timeout: TimeSpan.FromMinutes(1));

        var results = await collection.HybridSearchAsync(
            [
                new VectorAnnSearchRequest<float>(
                    "float_vector",
                    [(float[])[1f, 2f]],
                    SimilarityMetricType.L2,
                    limit: 3),
                new SparseVectorAnnSearchRequest<float>(
                    "sparse_vector",
                    [new MilvusSparseVector<float>((int[])[0, 1], (float[])[1.0f, 1.0f])],
                    SimilarityMetricType.Ip,
                    limit: 3)
            ],
            new RrfReranker(),
            limit: 3,
            new HybridSearchParameters { ConsistencyLevel = ConsistencyLevel.Strong });

        Assert.Equal(collection.Name, results.CollectionName);
        Assert.NotNull(results.Ids.LongIds);
        Assert.True(results.Ids.LongIds.Count > 0);
    }

    [Fact]
    public async Task HybridSearch_binary_vectors()
    {
        if (!SupportsHybridSearch)
        {
            return;
        }

        var collection = Client.GetCollection(nameof(HybridSearch_binary_vectors));
        await collection.DropAsync();

        await Client.CreateCollectionAsync(
            collection.Name,
            [
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2),
                FieldSchema.CreateBinaryVector("binary_vector", 16)
            ]);

        ReadOnlyMemory<byte>[] binaryVectors =
        [
            new byte[] { 0x01, 0x02 },
            new byte[] { 0x03, 0x04 },
            new byte[] { 0x05, 0x06 }
        ];

        await collection.InsertAsync([
            FieldData.Create("id", [1L, 2L, 3L]),
            FieldData.CreateFloatVector("float_vector", [
                new[] { 1f, 2f },
                new[] { 3f, 4f },
                new[] { 5f, 6f }
            ]),
            FieldData.CreateBinaryVectors("binary_vector", binaryVectors)
        ]);

        await collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);
        await collection.CreateIndexAsync("binary_vector", IndexType.BinFlat, SimilarityMetricType.Jaccard);

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100),
            timeout: TimeSpan.FromMinutes(1));

        var results = await collection.HybridSearchAsync(
            [
                new VectorAnnSearchRequest<float>(
                    "float_vector",
                    [new[] { 1f, 2f }],
                    SimilarityMetricType.L2,
                    limit: 3),
                new VectorAnnSearchRequest<byte>(
                    "binary_vector",
                    [binaryVectors[0]],
                    SimilarityMetricType.Jaccard,
                    limit: 3)
            ],
            new RrfReranker(),
            limit: 3,
            new HybridSearchParameters { ConsistencyLevel = ConsistencyLevel.Strong });

        Assert.Equal(collection.Name, results.CollectionName);
        Assert.NotNull(results.Ids.LongIds);
        Assert.True(results.Ids.LongIds.Count > 0);
    }

#if NET8_0_OR_GREATER
    [Fact]
    public async Task HybridSearch_float16_vectors()
    {
        if (await Client.GetParsedMilvusVersion() < new Version(2, 4))
        {
            return;
        }

        var collection = Client.GetCollection(nameof(HybridSearch_float16_vectors));
        await collection.DropAsync();

        await Client.CreateCollectionAsync(
            collection.Name,
            [
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2),
                FieldSchema.CreateFloat16Vector("float16_vector", 2)
            ]);

        ReadOnlyMemory<Half>[] float16Vectors =
        [
            new[] { (Half)1.0f, (Half)2.0f },
            new[] { (Half)3.0f, (Half)4.0f },
            new[] { (Half)5.0f, (Half)6.0f }
        ];

        await collection.InsertAsync([
            FieldData.Create("id", [1L, 2L, 3L]),
            FieldData.CreateFloatVector("float_vector", [
                new[] { 1f, 2f },
                new[] { 3f, 4f },
                new[] { 5f, 6f }
            ]),
            FieldData.CreateFloat16Vector("float16_vector", float16Vectors)
        ]);

        await collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);
        await collection.CreateIndexAsync("float16_vector", IndexType.Flat, SimilarityMetricType.L2);

        await collection.LoadAsync();
        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromMilliseconds(100),
            timeout: TimeSpan.FromMinutes(1));

        var results = await collection.HybridSearchAsync(
            [
                new VectorAnnSearchRequest<float>(
                    "float_vector",
                    [new[] { 1f, 2f }],
                    SimilarityMetricType.L2,
                    limit: 3),
                new VectorAnnSearchRequest<Half>(
                    "float16_vector",
                    [float16Vectors[0]],
                    SimilarityMetricType.L2,
                    limit: 3)
            ],
            new RrfReranker(),
            limit: 3,
            new HybridSearchParameters { ConsistencyLevel = ConsistencyLevel.Strong });

        Assert.Equal(collection.Name, results.CollectionName);
        Assert.NotNull(results.Ids.LongIds);
        Assert.True(results.Ids.LongIds.Count > 0);
    }
#endif

    [Fact]
    public void RrfReranker_default_k_is_60()
    {
        var reranker = new RrfReranker();
        Assert.Equal(60, reranker.K);
    }

    [Fact]
    public void RrfReranker_with_custom_k()
    {
        var reranker = new RrfReranker(100);
        Assert.Equal(100, reranker.K);
    }

    [Fact]
    public void RrfReranker_throws_for_k_less_than_1()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RrfReranker(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RrfReranker(-1));
    }

    [Fact]
    public void WeightedReranker_stores_weights()
    {
        var reranker = new WeightedReranker(0.7f, 0.3f);
        Assert.Equal([0.7f, 0.3f], reranker.Weights);
    }

    [Fact]
    public void WeightedReranker_throws_for_empty_weights()
    {
        Assert.Throws<ArgumentException>(() => new WeightedReranker());
    }

    [Fact]
    public async Task HybridSearch_throws_for_invalid_limit()
    {
        if (!SupportsHybridSearch)
        {
            return;
        }

        var requests = new AnnSearchRequest[]
        {
            new VectorAnnSearchRequest<float>(
                "float_vector_1",
                [new[] { 0.1f, 0.2f }],
                SimilarityMetricType.L2,
                limit: 3)
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Collection.HybridSearchAsync(
            requests,
            new RrfReranker(),
            limit: 0));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Collection.HybridSearchAsync(
            requests,
            new RrfReranker(),
            limit: 16385));
    }

    [Fact]
    public async Task HybridSearch_throws_for_mismatched_weighted_reranker()
    {
        if (!SupportsHybridSearch)
        {
            return;
        }

        var requests = new AnnSearchRequest[]
        {
            new VectorAnnSearchRequest<float>(
                "float_vector_1",
                [new[] { 0.1f, 0.2f }],
                SimilarityMetricType.L2,
                limit: 3),
            new VectorAnnSearchRequest<float>(
                "float_vector_2",
                [new[] { 0.3f, 0.4f }],
                SimilarityMetricType.L2,
                limit: 3)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => Collection.HybridSearchAsync(
            requests,
            new WeightedReranker(0.5f),
            limit: 3));
    }

    [Fact]
    public void AnnSearchRequest_ExtraParameters()
    {
        var request = new VectorAnnSearchRequest<float>(
            "vector_field",
            [new[] { 0.1f, 0.2f }],
            SimilarityMetricType.L2,
            limit: 10)
        {
            ExtraParameters =
            {
                ["nprobe"] = "10",
                ["ef"] = "64"
            }
        };

        Assert.Equal("10", request.ExtraParameters["nprobe"]);
        Assert.Equal("64", request.ExtraParameters["ef"]);
    }

    public class HybridSearchCollectionFixture : IAsyncLifetime
    {
        public HybridSearchCollectionFixture(MilvusFixture milvusFixture)
        {
            Client = milvusFixture.CreateClient();
            Collection = Client.GetCollection(nameof(HybridSearchTests));
        }

        private readonly MilvusClient Client;
        public readonly MilvusCollection Collection;
        public bool SupportsHybridSearch { get; private set; }

        public async Task InitializeAsync()
        {
            SupportsHybridSearch = await Client.GetParsedMilvusVersion() >= new Version(2, 4);
            if (!SupportsHybridSearch)
            {
                return;
            }

            await Collection.DropAsync();
            await Client.CreateCollectionAsync(
                Collection.Name,
                [
                    FieldSchema.Create<long>("id", isPrimaryKey: true),
                    FieldSchema.CreateVarchar("varchar", 256),
                    FieldSchema.CreateFloatVector("float_vector_1", 2),
                    FieldSchema.CreateFloatVector("float_vector_2", 2)
                ]);

            await Collection.CreateIndexAsync(
                "float_vector_1", IndexType.Flat, SimilarityMetricType.L2, "float_vector_1_idx");

            await Collection.CreateIndexAsync(
                "float_vector_2", IndexType.Flat, SimilarityMetricType.L2, "float_vector_2_idx");

            long[] ids = [1, 2, 3, 4, 5];
            string[] strings = ["one", "two", "three", "four", "five"];
            ReadOnlyMemory<float>[] floatVectors1 =
            [
                new[] { 1f, 2f },
                new[] { 3.5f, 4.5f },
                new[] { 5f, 6f },
                new[] { 7.7f, 8.8f },
                new[] { 9f, 10f }
            ];
            ReadOnlyMemory<float>[] floatVectors2 =
            [
                new[] { 0.1f, 0.2f },
                new[] { 0.3f, 0.4f },
                new[] { 0.5f, 0.6f },
                new[] { 0.7f, 0.8f },
                new[] { 0.9f, 1.0f }
            ];

            await Collection.InsertAsync(
            [
                FieldData.Create("id", ids),
                    FieldData.Create("varchar", strings),
                    FieldData.CreateFloatVector("float_vector_1", floatVectors1),
                    FieldData.CreateFloatVector("float_vector_2", floatVectors2)
            ]);

            await Collection.LoadAsync();
            await Collection.WaitForCollectionLoadAsync(
                waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));
        }

        public Task DisposeAsync()
        {
            Client.Dispose();
            return Task.CompletedTask;
        }
    }

    public void Dispose() => Client.Dispose();
}
