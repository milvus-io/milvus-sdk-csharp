namespace Milvus.Client;

/// <summary>
/// Constant/static values for internal usage.
/// </summary>
internal static class Constants
{
    /// <summary>
    /// Vector tag for <see cref="Grpc.PlaceholderValue"/>
    /// </summary>
    internal const string VectorTag = "$0";

    /// <summary>
    /// Key name in parameters.
    /// </summary>
    internal const string VectorField = "anns_field";

    /// <summary>
    /// Key name in parameters.
    /// </summary>
    internal const string VectorDim = "dim";

    /// <summary>
    /// Key name in parameters.Indicate the max length of varchar datatype.
    /// </summary>
    internal const string VarcharMaxLength = "max_length";

    /// <summary>
    /// Top parameter key name.
    /// </summary>
    internal const string TopK = "topk";

    /// <summary>
    /// Top parameter key name.
    /// </summary>
    internal const string Offset = "offset";

    /// <summary>
    /// Key name in parameters.<see cref="Client.IndexType"/>
    /// </summary>
    internal const string IndexType = "index_type";

    /// <summary>
    /// Key name in parameters.<see cref="SimilarityMetricType"/>
    /// </summary>
    internal const string MetricType = "metric_type";

    /// <summary>
    /// Key name in search parameters.
    /// </summary>
    internal const string RoundDecimal = "round_decimal";

    /// <summary>
    /// Key name.
    /// </summary>
    internal const string Params = "params";

    /// <summary>
    /// Row count key name.
    /// </summary>
    internal const string RowCount = "row_count";

    /// <summary>
    /// Key name.
    /// </summary>
    internal const string Bucket = "bucket";

    /// <summary>
    /// Key name.
    /// </summary>
    internal const string FailedReason = "failed_reason";

    /// <summary>
    /// Key name.
    /// </summary>
    internal const string MaxCapacity = "max_capacity";

    /// <summary>
    /// Files.
    /// </summary>
    internal const string ImportFiles = "files";

    /// <summary>
    /// Collection.
    /// </summary>
    internal const string ImportCollection = "collection";

    /// <summary>
    /// Partition.
    /// </summary>
    internal const string ImportPartition = "partition";

    /// <summary>
    /// Default index name.
    /// </summary>
    internal const string DefaultIndexName = "_default_idx";

    /// <summary>
    /// Key name.
    /// </summary>
    internal const string IgnoreGrowing = "ignore_growing";

    /// <summary>
    /// Key name.
    /// </summary>
    internal const string GroupByField = "group_by_field";

    /// <summary>
    /// Default database name.
    /// </summary>
    internal const string DefaultDatabaseName = "default";

    /// <summary>
    ///  max value for waiting loading collection/partition interval, unit: millisecond
    /// </summary>
    internal const long MaxWaitingLoadingInterval = 2000L;

    /// <summary>
    /// max value for waiting loading collection/partition timeout,  unit: second
    /// </summary>
    internal const long MaxWaitingLoadingTimeout = 300L;

    /// <summary>
    /// max value for waiting flushing collection/partition interval, unit: millisecond
    /// </summary>
    internal const long MaxWaitingFlushingInterval = 2000L;

    /// <summary>
    /// max value for waiting flushing collection/partition timeout,  unit: second
    /// </summary>
    internal const long MaxWaitingFlushingTimeout = 300L;

    /// <summary>
    /// max value for waiting create index interval, unit: millisecond
    /// </summary>
    internal const long MaxWaitingIndexInterval = 2000L;

    /// <summary>
    /// set this value for "withGuaranteeTimestamp" of QueryParam/SearchParam
    /// to instruct server execute query/search immediately.
    /// </summary>
    internal const long GuaranteeEventuallyTs = 1L;

    /// <summary>
    /// set this value for "withGuaranteeTimestamp" of QueryParam/SearchParam
    /// to instruct server execute query/search after all DML operations finished.
    /// </summary>
    internal const long GuaranteeStrongTs = 0L;
}
