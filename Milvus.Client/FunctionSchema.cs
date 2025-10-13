using System.Text.Json;

namespace Milvus.Client;

/// <summary>
/// A Function instance for generating vector embeddings from user-provided raw data or applying a reranking strategy to
/// the search results in Milvus.
/// </summary>
/// <remarks>
/// Api is pretty similar to Python one, so for details you may refer to
/// <see href="https://milvus.io/api-reference/pymilvus/v2.6.x/MilvusClient/Function/Function.md"/>.
/// </remarks>
public class FunctionSchema
{
    /// <summary>
    /// Unique identifier for this Function.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Function description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Function type.
    /// </summary>
    public required FunctionType Type { get; set; }

    /// <summary>
    /// List of vector fields to apply the function to.
    /// </summary>
    public required IReadOnlyList<string> InputFieldNames { get; set; }

    /// <summary>
    /// The name of the field where the generated embeddings will be stored. This should correspond to a vector field
    /// defined in the collection schema. This parameter accepts only one field name.
    /// </summary>
    /// <remarks>
    /// This applies only when you set function_type to <see cref="FunctionType.Bm25"/> and
    /// <see cref="FunctionType.TextEmbedding"/>.
    /// </remarks>
    public required IReadOnlyList<string> OutputFieldNames { get; set; }

    /// <summary>
    /// A configuration dictionary for the embedding/ranking function. Supported keys vary by <see cref="Type"/>.
    /// </summary>
    public required IReadOnlyDictionary<string, object>? Params { get; set; }

    internal Grpc.FunctionSchema ToGrpc()
    {
        Grpc.FunctionSchema schema = new()
        {
            Name = Name,
            Type = (Grpc.FunctionType)Type,
            InputFieldNames = { InputFieldNames },
            OutputFieldNames = { OutputFieldNames },
            Params = { EnumerateParams() }
        };

        if (Description != null)
        {
            schema.Description = Description;
        }

        return schema;
    }

    private IEnumerable<Grpc.KeyValuePair> EnumerateParams()
    {
        if (Params == null)
        {
            yield break;
        }

        foreach (var kvp in Params)
        {
            yield return new Grpc.KeyValuePair
            {
                Key = kvp.Key, Value = kvp.Value as string ?? JsonSerializer.Serialize(kvp.Value)
            };
        }
    }

    /// <summary>
    /// Constructs a Weighted ranker.
    /// </summary>
    /// <remarks>
    /// For more details, see <see href="https://milvus.io/docs/weighted-ranker.md" />.
    /// </remarks>
    /// <param name="name">
    /// Unique identifier for this Function.
    /// </param>
    /// <param name="weights">
    /// Array of weights corresponding to each search path; values ∈ [0,1]. For details, refer to
    /// <see href="https://milvus.io/docs/weighted-ranker.md#Mechanism-of-Weighted-Ranker">Mechanism of Weighted Ranker</see>.
    /// </param>
    /// <param name="description">Function description</param>
    /// <param name="normScore">
    /// Whether to normalize raw scores (using arctan) before weighting. For details, refer to
    /// <see href="https://milvus.io/docs/weighted-ranker.md#Mechanism-of-Weighted-Ranker">Mechanism of Weighted Ranker</see>.
    /// </param>
    /// <returns></returns>
    public static FunctionSchema WeightedRanker(string name,
        IReadOnlyList<float> weights,
        string? description = null,
        bool normScore = false)
    {
        return new FunctionSchema
        {
            Name = name,
            Description = description,
            Type = FunctionType.Rerank,
            InputFieldNames = [],
            OutputFieldNames = [],
            Params = new Dictionary<string, object>
            {
                ["reranker"] = RankerType.Weighted, ["weights"] = weights, ["norm_score"] = normScore
            }
        };
    }

    /// <summary>
    /// Constructs a Reciprocal Rank Fusion ranker.
    /// </summary>
    /// <remarks>
    /// For more details, see <see href="https://milvus.io/docs/rrf-ranker.md" />.
    /// </remarks>
    /// <param name="name">
    /// Unique identifier for this Function.
    /// </param>
    /// <param name="description">
    /// Function description.
    /// </param>
    /// <param name="k">
    /// Smoothing parameter that controls the impact of document ranks; higher k reduces sensitivity to top ranks.
    /// Range: (0, 16384); default: 60. For details refere to
    /// <see href="https://milvus.io/docs/rrf-ranker.md#Mechanism-of-RRF-Ranker">Mechanism of RRF Ranker</see>.
    /// </param>
    /// <returns></returns>
    public static FunctionSchema RRFRanker(string name, string? description = null, int k = 60)
    {
        return new FunctionSchema
        {
            Name = name,
            Description = description,
            Type = FunctionType.Rerank,
            InputFieldNames = [],
            OutputFieldNames = [],
            Params = new Dictionary<string, object> { ["reranker"] = RankerType.RRF, ["k"] = k }
        };
    }
}
