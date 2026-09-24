using System.Globalization;
using System.Text.Json;

using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// Interface for rerankers used in hybrid search to combine results from multiple vector searches.
/// </summary>
public interface IReranker
{
    /// <summary>
    /// Gets the rank parameters describing this reranker's strategy (used for <c>rank_params</c>-based
    /// strategies such as RRF and weighted).
    /// </summary>
    IReadOnlyList<KeyValuePair<string, string>> ToRankParams();

    /// <summary>
    /// Gets a structured <see cref="FunctionScore" /> for this reranker, or <c>null</c> when the reranker
    /// uses the <c>rank_params</c> channel instead (RRF / weighted). Function-score rerankers (boost, decay,
    /// model) are mapped to the proto <c>schema.FunctionScore</c>.
    /// </summary>
    FunctionScore? ToFunctionScore();
}

/// <summary>
/// Reciprocal Rank Fusion (RRF) reranker that combines results based on their reciprocal ranks.
/// </summary>
/// <remarks>
/// RRF is a popular technique for combining ranked lists. The score for each document is computed as:
/// <c>score = sum(1 / (k + rank))</c> across all input searches, where <c>k</c> is a constant (default 60).
/// </remarks>
public sealed class RrfReranker : IReranker
{
    /// <summary>
    /// Creates a new RRF reranker with the default k value of 60.
    /// </summary>
    public RrfReranker()
        : this(60)
    {
    }

    /// <summary>
    /// Creates a new RRF reranker with a specified k value.
    /// </summary>
    /// <param name="k">The constant used in the RRF formula. Must be greater than or equal to 1.</param>
    public RrfReranker(float k)
    {
        // float.IsFinite is unavailable on netstandard2.0/net462.
        if (float.IsNaN(k) || float.IsInfinity(k))
        {
            throw new ArgumentOutOfRangeException(nameof(k), k, "k must be a finite number");
        }

        if (k < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(k), k, "k must be greater than or equal to 1");
        }

        K = k;
    }

    /// <summary>
    /// The constant used in the RRF formula. Higher values give more weight to lower-ranked results.
    /// </summary>
    public float K { get; }

    /// <inheritdoc />
    public IReadOnlyList<KeyValuePair<string, string>> ToRankParams()
        => new[]
        {
            new KeyValuePair<string, string>("strategy", "rrf"),
            new KeyValuePair<string, string>("params", $"{{\"k\": {K.ToString(CultureInfo.InvariantCulture)}}}")
        };

    /// <inheritdoc />
    public FunctionScore? ToFunctionScore() => null;
}

/// <summary>
/// Weighted reranker that combines results using specified weights for each input search.
/// </summary>
/// <remarks>
/// The final score for each document is computed as a weighted sum of its scores from each input search.
/// </remarks>
public sealed class WeightedReranker : IReranker
{
    /// <summary>
    /// Creates a new weighted reranker with the specified weights.
    /// </summary>
    /// <param name="weights">
    /// The weights to apply to each input search. The number of weights must match the number of ANN search requests.
    /// </param>
    public WeightedReranker(params float[] weights)
    {
        Verify.NotNull(weights);

        if (weights.Length == 0)
        {
            throw new ArgumentException("At least one weight must be provided", nameof(weights));
        }

        if (weights.Any(w => float.IsNaN(w) || float.IsInfinity(w)))
        {
            throw new ArgumentException("All weights must be finite numbers", nameof(weights));
        }

        Weights = weights;
    }

    /// <summary>
    /// The weights to apply to each input search. Must have the same number of elements as there are input searches.
    /// </summary>
    public IReadOnlyList<float> Weights { get; }

    /// <inheritdoc />
    public IReadOnlyList<KeyValuePair<string, string>> ToRankParams()
        => new[]
        {
            new KeyValuePair<string, string>("strategy", "weighted"),
            new KeyValuePair<string, string>("params",
                $"{{\"weights\": [{string.Join(", ", Weights.Select(w => w.ToString(CultureInfo.InvariantCulture)))}]}}")
        };

    /// <inheritdoc />
    public FunctionScore? ToFunctionScore() => null;
}

/// <summary>
/// Base for function-score (boost/decay/model) rerankers. Exposes the rerank function's name and parameters,
/// and converts to a <see cref="FunctionScore" /> for the proto <c>schema.FunctionScore</c> wire member.
/// </summary>
public abstract class FunctionReranker<T> : IReranker
    where T : FunctionReranker<T>
{
    private readonly string _name;

    /// <summary>
    /// Creates a function reranker with the given function name and type (typically
    /// <see cref="FunctionType.Rerank" />).
    /// </summary>
    protected FunctionReranker(string name, FunctionType type, string rerankerName)
    {
        Verify.NotNullOrWhiteSpace(name);
        _name = name;
        Type = type;
        RerankerName = rerankerName;
    }

    /// <summary>
    /// The function type (always <see cref="FunctionType.Rerank" /> for rerankers).
    /// </summary>
    public FunctionType Type { get; }

    /// <summary>
    /// The rerank strategy name ("boost" / "decay" / "model") the server identifies the function by.
    /// </summary>
    public string RerankerName { get; }

    /// <summary>
    /// The rerank function's parameters (boost/decay/model settings).
    /// </summary>
    public IDictionary<string, string> Params { get; } = new Dictionary<string, string>();

    /// <summary>
    /// The input field names the rerank function operates on (e.g. the decay field); decay/model require one.
    /// </summary>
    public IList<string> InputFieldNames { get; } = new List<string>();

    /// <summary>
    /// Sets a parameter and returns this instance (fluent).
    /// </summary>
    protected T WithParam(string key, string value)
    {
        Params[key] = value;
        return (T)this;
    }

    /// <inheritdoc />
    // Function-score rerankers use the function_score wire channel, not rank_params.
    public IReadOnlyList<KeyValuePair<string, string>> ToRankParams()
        => Array.Empty<KeyValuePair<string, string>>();

    /// <inheritdoc />
    public FunctionScore? ToFunctionScore()
    {
        var score = new FunctionScore();
        var function = new FunctionSchema(_name, Type, InputFieldNames, Array.Empty<string>())
        {
            // The server dispatches the rerank strategy from the "reranker" function param; without it the
            // proxy fails with "reranker name not specified" (mirrors the C++ Function::SetFunctionType).
            Params = Params.Count > 0 ? new Dictionary<string, string>(Params) : new Dictionary<string, string>()
        };
        function.Params["reranker"] = RerankerName;
        score.Functions.Add(function);
        return score;
    }
}

/// <summary>
/// A function-score reranker that boosts results matching a filter (or random score on a field). Mapped to the
/// proto <c>schema.FunctionScore</c>. Mirrors the C++ SDK's <c>BoostRerank</c> / Java <c>Function</c>.
/// </summary>
public sealed class BoostRerank : FunctionReranker<BoostRerank>
{
    /// <summary>
    /// Creates a boost reranker.
    /// </summary>
    public BoostRerank(string name)
        : base(name, FunctionType.Rerank, "boost")
    {
    }

    /// <summary>
    /// Sets the filter expression; results matching it get the boost.
    /// </summary>
    public BoostRerank WithFilter(string filter) => WithParam("filter", filter);

    /// <summary>
    /// Sets the boost weight applied to matching results.
    /// </summary>
    public BoostRerank WithWeight(float weight) => WithParam("weight", weight.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Sets the field used to generate a random score. The server reads a single <c>random_score</c> JSON
    /// param with <c>field</c>/<c>seed</c> sub-keys, so both settings merge into one JSON object.
    /// </summary>
    public BoostRerank WithRandomScoreField(string field) => MergeRandomScore(field: field, seed: null);

    /// <summary>
    /// Sets the seed for the random score.
    /// </summary>
    public BoostRerank WithRandomScoreSeed(long seed) => MergeRandomScore(field: null, seed: seed);

    private BoostRerank MergeRandomScore(string? field, long? seed)
    {
        using var document = Params.TryGetValue("random_score", out string? existing)
            ? System.Text.Json.JsonDocument.Parse(existing)
            : System.Text.Json.JsonDocument.Parse("{}");
        var dict = new Dictionary<string, object?>();
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            dict[property.Name] = property.Value.Clone();
        }

        if (field is not null)
        {
            dict["field"] = field;
        }

        if (seed is not null)
        {
            // The server parses random_score with json.Decoder.UseNumber() and requires seed to be a JSON
            // number, so keep it as a numeric long, not a quoted string.
            dict["seed"] = seed.Value;
        }

        Params["random_score"] = System.Text.Json.JsonSerializer.Serialize(dict);
        return this;
    }
}

/// <summary>
/// A function-score reranker applying a gaussian/exponential/linear decay to results by a scalar field. Mapped
/// to the proto <c>schema.FunctionScore</c>. Mirrors the C++ SDK's <c>DecayRerank</c>.
/// </summary>
public sealed class DecayRerank : FunctionReranker<DecayRerank>
{
    /// <summary>
    /// Creates a decay reranker.
    /// </summary>
    public DecayRerank(string name)
        : base(name, FunctionType.Rerank, "decay")
    {
    }

    /// <summary>
    /// Sets the field the decay applies to; the server requires exactly one input field for decay functions.
    /// </summary>
    public DecayRerank WithInputField(string field)
    {
        InputFieldNames.Add(field);
        return this;
    }

    /// <summary>
    /// Sets the decay function: <c>"gauss"</c>, <c>"exp"</c> or <c>"linear"</c>.
    /// </summary>
    public DecayRerank WithFunction(string function) => WithParam("function", function);

    /// <summary>
    /// Sets the reference point (origin) of the decay curve.
    /// </summary>
    public DecayRerank WithOrigin<T>(T origin) => WithParam("origin", FormatNumeric(origin));

    /// <summary>
    /// Sets the no-decay zone around the origin.
    /// </summary>
    public DecayRerank WithOffset<T>(T offset) => WithParam("offset", FormatNumeric(offset));

    /// <summary>
    /// Sets the scale: the position at which relevance drops to the decay value.
    /// </summary>
    public DecayRerank WithScale<T>(T scale) => WithParam("scale", FormatNumeric(scale));

    /// <summary>
    /// Sets the decay value (the score at the scale position).
    /// </summary>
    public DecayRerank WithDecay(float decay) => WithParam("decay", decay.ToString(CultureInfo.InvariantCulture));

    // Numeric decay parameters must be serialized with the invariant culture so a comma-decimal locale cannot
    // corrupt the wire value (e.g. "2,5"); strings pass through unchanged.
    private static string FormatNumeric<T>(T value)
        => value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value?.ToString() ?? "";
}

/// <summary>
/// A function-score reranker that uses a model service to recompute relevance. Mapped to the proto
/// <c>schema.FunctionScore</c>. Mirrors the C++ SDK's <c>ModelRerank</c>.
/// </summary>
public sealed class ModelRerank : FunctionReranker<ModelRerank>
{
    /// <summary>
    /// Creates a model reranker.
    /// </summary>
    public ModelRerank(string name)
        : base(name, FunctionType.Rerank, "model")
    {
    }

    /// <summary>
    /// Sets the model service provider.
    /// </summary>
    public ModelRerank WithProvider(string provider) => WithParam("provider", provider);

    /// <summary>
    /// Sets the query strings used by the model to compute relevance. The count must equal the number of
    /// queries in the search.
    /// </summary>
    public ModelRerank WithQueries(IEnumerable<string> queries)
    {
        Params["queries"] = System.Text.Json.JsonSerializer.Serialize(queries.ToArray());
        return this;
    }

    /// <summary>
    /// Sets the model service endpoint URL. The server reads this under the <c>endpoint</c> function param
    /// key (the vllm/tei providers match <c>endpoint</c>), matching the C++/Java SDKs.
    /// </summary>
    public ModelRerank WithEndpoint(string endpoint) => WithParam("endpoint", endpoint);
}
