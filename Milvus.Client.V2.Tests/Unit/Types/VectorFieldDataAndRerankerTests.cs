using Xunit;

using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Types;

[Trait("Category", "Unit")]
public class VectorFieldDataAndRerankerTests
{
    [Fact]
    public void BinaryVectorFieldData_serializes_bytes_and_bit_dim()
    {
        var field = FieldData.CreateBinaryVectors("vec", new[]
        {
            new ReadOnlyMemory<byte>(new byte[] { 0x0F, 0xF0 }),
            new ReadOnlyMemory<byte>(new byte[] { 0xAA, 0x55 })
        });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(Grpc.DataType.BinaryVector, grpc.Type);
        Assert.Equal(16, grpc.Vectors.Dim); // 2 bytes * 8 bits per row
        Assert.Equal(new byte[] { 0x0F, 0xF0, 0xAA, 0x55 }, grpc.Vectors.BinaryVector.ToByteArray());
    }

    [Fact]
    public void SparseFloatVectorFieldData_serializes_rows_and_max_dim()
    {
        var field = FieldData.CreateSparseFloatVector("vec", new[]
        {
            new MilvusSparseVector<float>(new[] { 0, 3 }, new[] { 1.5f, 2.5f }),
            new MilvusSparseVector<float>(new[] { 5 }, new[] { 3.5f })
        });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(Grpc.DataType.SparseFloatVector, grpc.Type);
        Assert.Equal(2, grpc.Vectors.SparseFloatVector.Contents.Count);
        Assert.Equal(6, grpc.Vectors.Dim); // max index (5) + 1

        MilvusSparseVector<float> row0 = MilvusSparseVector<float>.FromBytes(grpc.Vectors.SparseFloatVector.Contents[0].Span);
        Assert.Equal(new[] { 0, 3 }, row0.Indices.ToArray());
        Assert.Equal(new[] { 1.5f, 2.5f }, row0.Values.ToArray());

        MilvusSparseVector<float> row1 = MilvusSparseVector<float>.FromBytes(grpc.Vectors.SparseFloatVector.Contents[1].Span);
        Assert.Equal(new[] { 5 }, row1.Indices.ToArray());
        Assert.Equal(new[] { 3.5f }, row1.Values.ToArray());
    }

    [Fact]
    public void Int8VectorFieldData_serializes_rows_as_bytes()
    {
        var field = new Int8VectorFieldData("vec", new[]
        {
            new ReadOnlyMemory<sbyte>(new sbyte[] { 0, -1, 127, -128 }),
            new ReadOnlyMemory<sbyte>(new sbyte[] { 1, 2, -3, 4 })
        });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(Grpc.DataType.Int8Vector, grpc.Type);
        Assert.Equal(4, grpc.Vectors.Dim);
        Assert.Equal(new byte[] { 0x00, 0xFF, 0x7F, 0x80, 0x01, 0x02, 0xFD, 0x04 }, grpc.Vectors.Int8Vector.ToByteArray());
    }

    [Fact]
    public void VectorFieldData_rejects_mismatched_row_dimensions()
    {
        var field = new Int8VectorFieldData("vec", new[]
        {
            new ReadOnlyMemory<sbyte>(new sbyte[] { 0, 1, 2, 3 }),
            new ReadOnlyMemory<sbyte>(new sbyte[] { 4, 5 })
        });

        Assert.Throws<ArgumentException>(() => field.ToGrpcFieldData());
    }

    [Fact]
    public void RrfReranker_uses_default_k_of_60()
    {
        var reranker = new RrfReranker();

        Assert.Equal(60f, reranker.K);

        IReadOnlyList<KeyValuePair<string, string>> param = reranker.ToRankParams();
        Assert.Equal("rrf", param[0].Value);
        Assert.Equal("params", param[1].Key);
        Assert.Contains("\"k\": 60", param[1].Value);
    }

    [Fact]
    public void RrfReranker_rejects_k_below_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RrfReranker(0.5f));
    }

    [Fact]
    public void RrfReranker_rejects_nan_and_infinity_k()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RrfReranker(float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RrfReranker(float.PositiveInfinity));
    }

    [Fact]
    public void WeightedReranker_requires_at_least_one_weight()
    {
        Assert.Throws<ArgumentException>(() => new WeightedReranker());
    }

    [Fact]
    public void WeightedReranker_rejects_nan_and_infinity_weights()
    {
        Assert.Throws<ArgumentException>(() => new WeightedReranker(0.3f, float.NaN));
        Assert.Throws<ArgumentException>(() => new WeightedReranker(float.NegativeInfinity));
    }

    [Fact]
    public void WeightedReranker_maps_params()
    {
        var reranker = new WeightedReranker(0.3f, 0.7f);

        IReadOnlyList<KeyValuePair<string, string>> param = reranker.ToRankParams();
        Assert.Equal("weighted", param[0].Value);
        Assert.Equal("params", param[1].Key);
        Assert.Contains("[0.3, 0.7]", param[1].Value);
    }

    [Fact]
    public void Rerankers_implement_common_interface()
    {
        IReranker rrf = new RrfReranker();
        IReranker weighted = new WeightedReranker(1f);

        Assert.Equal("rrf", rrf.ToRankParams()[0].Value);
        Assert.Equal("weighted", weighted.ToRankParams()[0].Value);
    }

    [Fact]
    public void BoostRerank_builds_params_and_function_score()
    {
        var reranker = new BoostRerank("boost_fn")
            .WithFilter("age > 30")
            .WithWeight(1.5f);

        Assert.Equal(FunctionType.Rerank, reranker.Type);
        Assert.Equal("boost", reranker.RerankerName);
        Assert.Equal("age > 30", reranker.Params["filter"]);
        Assert.Equal("1.5", reranker.Params["weight"]);

        FunctionScore? score = reranker.ToFunctionScore();
        Assert.NotNull(score);
        FunctionSchema function = Assert.Single(score.Functions);
        Assert.Equal("boost_fn", function.Name);
        Assert.Equal(FunctionType.Rerank, function.Type);
        Assert.Equal("boost", function.Params!["reranker"]);
    }

    [Fact]
    public void BoostRerank_merges_random_score_field_and_seed()
    {
        var reranker = new BoostRerank("boost_fn")
            .WithRandomScoreField("extra")
            .WithRandomScoreSeed(42);

        string randomScore = reranker.Params["random_score"];
        Assert.Contains("\"field\":\"extra\"", randomScore);
        Assert.Contains("\"seed\":42", randomScore);

        // Setting field after seed keeps both (merge, not overwrite).
        reranker.WithRandomScoreField("other");
        Assert.Contains("\"seed\":42", reranker.Params["random_score"]);
    }

    [Fact]
    public void DecayRerank_builds_params_and_function_score()
    {
        var reranker = new DecayRerank("decay_fn")
            .WithInputField("price")
            .WithFunction("gauss")
            .WithOrigin(100.0)
            .WithOffset(10.0)
            .WithScale(50.0)
            .WithDecay(0.5f);

        Assert.Equal("decay", reranker.RerankerName);
        Assert.Equal(new[] { "price" }, reranker.InputFieldNames);
        Assert.Equal("gauss", reranker.Params["function"]);
        Assert.Equal("100", reranker.Params["origin"]);
        Assert.Equal("10", reranker.Params["offset"]);
        Assert.Equal("50", reranker.Params["scale"]);
        Assert.Equal("0.5", reranker.Params["decay"]);

        FunctionSchema function = Assert.Single(reranker.ToFunctionScore()!.Functions);
        Assert.Equal(new[] { "price" }, function.InputFieldNames);
        Assert.Equal("decay", function.Params!["reranker"]);
    }

    [Fact]
    public void DecayRerank_formats_decimals_with_invariant_culture()
    {
        var reranker = new DecayRerank("decay_fn")
            .WithOrigin(2.5);

        Assert.Equal("2.5", reranker.Params["origin"]);
    }

    [Fact]
    public void ModelRerank_builds_params_and_function_score()
    {
        var reranker = new ModelRerank("model_fn")
            .WithProvider("vllm")
            .WithQueries(new[] { "what is milvus?" })
            .WithEndpoint("http://localhost:8000/v1/rerank");

        Assert.Equal("model", reranker.RerankerName);
        Assert.Equal("vllm", reranker.Params["provider"]);
        Assert.Contains("what is milvus?", reranker.Params["queries"]);
        Assert.Equal("http://localhost:8000/v1/rerank", reranker.Params["endpoint"]);

        FunctionSchema function = Assert.Single(reranker.ToFunctionScore()!.Functions);
        Assert.Equal("model", function.Params!["reranker"]);
    }

    [Fact]
    public void Rrf_and_weighted_have_no_function_score()
    {
        Assert.Null(new RrfReranker().ToFunctionScore());
        Assert.Null(new WeightedReranker(1f).ToFunctionScore());
    }
}
