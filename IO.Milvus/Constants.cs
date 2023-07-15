namespace IO.Milvus;

/// <summary>
/// Constant/static values for internal usage.
/// </summary>
public static class Constants
{
    // TODO: Decide whether we want to publicly expose these constants.

    /// <summary>
    /// Vector tag for <see cref="Grpc.PlaceholderValue"/>
    /// </summary>
    public const string VectorTag = "$0";

    /// <summary>
    /// Key name in parameters.
    /// </summary>
    public const string VectorField = "anns_field";

    /// <summary>
    /// Key name in parameters.
    /// </summary>
    public const string VectorDim = "dim";

    /// <summary>
    /// Key name in parameters.Indicate the max length of varchar datatype.
    /// </summary>
    public const string VarcharMaxLength = "max_length";

    /// <summary>
    /// Top parameter key name.
    /// </summary>
    public const string TopK = "topk";

    /// <summary>
    /// Key name in parameters.<see cref="MilvusIndexType"/>
    /// </summary>
    public const string IndexType = "index_type";

    /// <summary>
    /// Key name in parameters.<see cref="MilvusMetricType"/>
    /// </summary>
    public const string MetricType = "metric_type";

    /// <summary>
    /// Key name in search parameters.
    /// </summary>
    public const string RoundDecimal = "round_decimal";

    /// <summary>
    /// Key name.
    /// </summary>
    public const string Params = "params";

    /// <summary>
    /// Row count key name.
    /// </summary>
    public const string RowCount = "row_count";

    /// <summary>
    /// Key name.
    /// </summary>
    public const string Bucket = "bucket";

    /// <summary>
    /// Key name.
    /// </summary>
    public const string FailedReason = "failed_reason";

    /// <summary>
    /// Files.
    /// </summary>
    public const string ImportFiles = "files";

    /// <summary>
    /// Collection.
    /// </summary>
    public const string ImportCollection = "collection";

    /// <summary>
    /// Partition.
    /// </summary>
    public const string ImportPartition = "partition";

    /// <summary>
    /// Default index name.
    /// </summary>
    public const string DefaultIndexName = "_default_idx";

    /// <summary>
    /// Key name.
    /// </summary>
    public const string IgnoreGrowing = "ignore_growing";

    /// <summary>
    /// Default database name.
    /// </summary>
    public const string DefaultDatabaseName = "default";

    /// <summary>
    ///  max value for waiting loading collection/partition interval, unit: millisecond
    /// </summary>
    public const long MaxWaitingLoadingInterval = 2000L;

    /// <summary>
    /// max value for waiting loading collection/partition timeout,  unit: second
    /// </summary>
    public const long MaxWaitingLoadingTimeout = 300L;

    /// <summary>
    /// max value for waiting flushing collection/partition interval, unit: millisecond
    /// </summary>
    public const long MaxWaitingFlushingInterval = 2000L;

    /// <summary>
    /// max value for waiting flushing collection/partition timeout,  unit: second
    /// </summary>
    public const long MaxWaitingFlushingTimeout = 300L;

    /// <summary>
    /// max value for waiting create index interval, unit: millisecond
    /// </summary>
    public const long MaxWaitingIndexInterval = 2000L;

    /// <summary>
    /// set this value for "withGuaranteeTimestamp" of QueryParam/SearchParam
    /// to instruct server execute query/search immediately.
    /// </summary>
    public const long GuaranteeEventuallyTs = 1L;

    /// <summary>
    /// set this value for "withGuaranteeTimestamp" of QueryParam/SearchParam
    /// to instruct server execute query/search after all DML operations finished.
    /// </summary>
    public const long GuaranteeStrongTs = 0L;
}
