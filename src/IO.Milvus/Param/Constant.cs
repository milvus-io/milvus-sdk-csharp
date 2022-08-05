namespace IO.Milvus.Param
{
    /// <summary>
    /// Constant/static values for internal usage.
    /// </summary>
    public class Constant
    {
        public const string VECTOR_TAG = "$0";
        public const string VECTOR_FIELD = "anns_field";
        public const string VECTOR_DIM = "dim";
        public const string VARCHAR_MAX_LENGTH = "max_length";
        public const string TOP_K = "topk";
        public const string INDEX_TYPE = "index_type";
        public const string METRIC_TYPE = "metric_type";
        public const string ROUND_DECIMAL = "round_decimal";
        public const string PARAMS = "params";
        public const string ROW_COUNT = "row_count";
        public const string BUCKET = "bucket";
        public const string FAILED_REASON = "failed_reason";
        public const string IMPORT_FILES = "files";
        public const string IMPORT_COLLECTION = "collection";
        public const string IMPORT_PARTITION = "partition";
        public const string DEFAULT_INDEX_NAME = "_default_idx";

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
}
