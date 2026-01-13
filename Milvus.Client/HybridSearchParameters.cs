namespace Milvus.Client;

/// <summary>
/// A set of optional parameters for performing a hybrid search via
/// <see cref="MilvusCollection.HybridSearchAsync" />.
/// </summary>
public class HybridSearchParameters
{
    internal List<string>? OutputFieldsInternal { get; private set; }
    internal List<string>? PartitionNamesInternal { get; private set; }

    /// <inheritdoc cref="SearchParameters.PartitionNames"/>
    public IList<string> PartitionNames => PartitionNamesInternal ??= [];

    /// <inheritdoc cref="SearchParameters.OutputFields"/>
    public IList<string> OutputFields => OutputFieldsInternal ??= [];

    /// <inheritdoc cref="SearchParameters.ConsistencyLevel"/>
    public ConsistencyLevel? ConsistencyLevel { get; set; }

    /// <inheritdoc cref="SearchParameters.GuaranteeTimestamp"/>
    public ulong? GuaranteeTimestamp { get; set; }

    /// <inheritdoc cref="SearchParameters.TimeTravelTimestamp"/>
    public ulong? TimeTravelTimestamp { get; set; }

    /// <inheritdoc cref="SearchParameters.RoundDecimal"/>
    public long? RoundDecimal { get; set; }

    /// <inheritdoc cref="SearchParameters.GroupByField"/>
    public string? GroupByField { get; set; }

    /// <inheritdoc cref="SearchParameters.GroupSize"/>
    public int? GroupSize { get; set; }

    /// <inheritdoc cref="SearchParameters.StrictGroupSize"/>
    public bool? StrictGroupSize { get; set; }
}
