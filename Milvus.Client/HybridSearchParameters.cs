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

    /// <summary>
    /// Specifies the desired number of entities to return per group. Used in conjunction with
    /// <see cref="GroupByField"/>. If not set, the system defaults to returning one result per group.
    /// </summary>
    /// <remarks>
    /// See <see href="https://milvus.io/docs/grouping-search.md" /> for more information.
    /// </remarks>
    public int? GroupSize { get; set; }

    /// <summary>
    /// Controls whether the system should strictly enforce the count set by <see cref="GroupSize"/>.
    /// When <c>true</c>, the system will attempt to include the exact number of entities specified by
    /// <see cref="GroupSize"/> in each group. When <c>false</c> (the default), the system prioritizes
    /// meeting the number of groups specified by the limit parameter.
    /// </summary>
    /// <remarks>
    /// See <see href="https://milvus.io/docs/grouping-search.md" /> for more information.
    /// </remarks>
    public bool? StrictGroupSize { get; set; }
}
