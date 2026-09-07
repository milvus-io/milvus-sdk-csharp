namespace Milvus.Client.V2.Utils;

/// <summary>
/// Constant/static values for internal usage.
/// </summary>
internal static class Constants
{
    /// <summary>
    /// Vector tag for <see cref="Grpc.PlaceholderValue" />.
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
    /// Key name in parameters. Indicate the max length of varchar datatype.
    /// </summary>
    internal const string VarcharMaxLength = "max_length";

    /// <summary>
    /// Top parameter key name.
    /// </summary>
    internal const string TopK = "topk";

    /// <summary>
    /// Key name in parameters.
    /// </summary>
    internal const string Offset = "offset";

    /// <summary>
    /// Top parameter key name.
    /// </summary>
    internal const string Limit = "limit";

    /// <summary>
    /// Top parameter key name.
    /// </summary>
    internal const string BatchSize = "batch_size";

    /// <summary>
    /// Key name in parameters.
    /// </summary>
    internal const string Iterator = "iterator";

    /// <summary>
    /// Reduce stop for best parameter key name.
    /// </summary>
    internal const string ReduceStopForBest = "reduce_stop_for_best";

    /// <summary>
    /// Key name in parameters.
    /// </summary>
    internal const string IndexType = "index_type";

    /// <summary>
    /// Key name in parameters.
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
    /// Key name in type params. Indicates whether an analyzer is enabled for a varchar field.
    /// </summary>
    internal const string EnableAnalyzer = "enable_analyzer";

    /// <summary>
    /// Key name in type params. Contains the analyzer parameters as JSON.
    /// </summary>
    internal const string AnalyzerParams = "analyzer_params";

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
    /// Key name.
    /// </summary>
    internal const string GroupSize = "group_size";

    /// <summary>
    /// Key name.
    /// </summary>
    internal const string StrictGroupSize = "strict_group_size";

    /// <summary>
    /// Key name.
    /// </summary>
    internal const string GracefulTime = "graceful_time";

    /// <summary>
    /// Default database name.
    /// </summary>
    internal const string DefaultDatabaseName = "default";

    /// <summary>
    /// Max value for waiting collection/partition loading interval, in milliseconds.
    /// </summary>
    internal const long MaxWaitingLoadingInterval = 2000L;

    /// <summary>
    /// Max value for waiting collection/partition loading timeout, in seconds.
    /// </summary>
    internal const long MaxWaitingLoadingTimeout = 300L;

    /// <summary>
    /// Set this value for the "guaranteeTimestamp" to instruct the server to execute the query/search immediately.
    /// </summary>
    internal const long GuaranteeEventuallyTs = 1L;

    /// <summary>
    /// Set this value for the "guaranteeTimestamp" to instruct the server to execute the query/search after all DML
    /// operations finished.
    /// </summary>
    internal const long GuaranteeStrongTs = 0L;
}
