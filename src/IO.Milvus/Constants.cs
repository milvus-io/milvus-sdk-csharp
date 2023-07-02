namespace IO.Milvus;

/// <summary>
/// Constant/static values for internal usage.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Vector tag for <see cref="Grpc.PlaceholderValue"/>
    /// </summary>
    public const string VECTOR_TAG = "$0";

    /// <summary>
    /// Key name in parameters.
    /// </summary>
    public const string VECTOR_FIELD = "anns_field";

    /// <summary>
    /// Key name in parameters.
    /// </summary>
    public const string VECTOR_DIM = "dim";

    /// <summary>
    /// Key name in parameters.Indicate the max length of varchar datatype.
    /// </summary>
    public const string VARCHAR_MAX_LENGTH = "max_length";

    /// <summary>
    /// Top parameter key name.
    /// </summary>
    public const string TOP_K = "topk";

    /// <summary>
    /// Key name in parameters.<see cref="MilvusIndexType"/>
    /// </summary>
    public const string INDEX_TYPE = "index_type";

    /// <summary>
    /// Key name in parameters.<see cref="MilvusMetricType"/>
    /// </summary>
    public const string METRIC_TYPE = "metric_type";

    /// <summary>
    /// Key name in search parameters.
    /// </summary>
    public const string ROUND_DECIMAL = "round_decimal";

    /// <summary>
    /// Key name.
    /// </summary>
    public const string PARAMS = "params";

    /// <summary>
    /// Row count key name.
    /// </summary>
    public const string ROW_COUNT = "row_count";

    /// <summary>
    /// Key name.
    /// </summary>
    public const string BUCKET = "bucket";

    /// <summary>
    /// Key name.
    /// </summary>
    public const string FAILED_REASON = "failed_reason";

    /// <summary>
    /// Files.
    /// </summary>
    public const string IMPORT_FILES = "files";

    /// <summary>
    /// Collection.
    /// </summary>
    public const string IMPORT_COLLECTION = "collection";

    /// <summary>
    /// Partition.
    /// </summary>
    public const string IMPORT_PARTITION = "partition";

    /// <summary>
    /// Default index name.
    /// </summary>
    public const string DEFAULT_INDEX_NAME = "_default_idx";

    /// <summary>
    /// Key name.
    /// </summary>
    public const string IGNORE_GROWING = "ignore_growing";

    /// <summary>
    /// Default database name.
    /// </summary>
    public const string DEFAULT_DATABASE_NAME = "default";

    /// <summary>
    ///  max value for waiting loading collection/partition interval, unit: millisecond
    /// </summary>
    public const long MAX_WAITING_LOADING_INTERVAL = 2000L;

    /// <summary>
    /// max value for waiting loading collection/partition timeout,  unit: second
    /// </summary>
    public const long MAX_WAITING_LOADING_TIMEOUT = 300L;

    /// <summary>
    /// max value for waiting flushing collection/partition interval, unit: millisecond
    /// </summary>
    public const long MAX_WAITING_FLUSHING_INTERVAL = 2000L;

    /// <summary>
    /// max value for waiting flushing collection/partition timeout,  unit: second
    /// </summary>
    public const long MAX_WAITING_FLUSHING_TIMEOUT = 300L;

    /// <summary>
    /// max value for waiting create index interval, unit: millisecond
    /// </summary>
    public const long MAX_WAITING_INDEX_INTERVAL = 2000L;

    /// <summary>
    /// set this value for "withGuaranteeTimestamp" of QueryParam/SearchParam
    /// to instruct server execute query/search immediately. 
    /// </summary>
    public const long GUARANTEE_EVENTUALLY_TS = 1L;

    /// <summary>
    /// set this value for "withGuaranteeTimestamp" of QueryParam/SearchParam
    /// to instruct server execute query/search after all DML operations finished.
    /// </summary>
    public const long GUARANTEE_STRONG_TS = 0L;
}
