namespace Milvus.Client.V2;

/// <summary>
/// An error code returned in <see cref="MilvusException.ErrorCode" />.
/// </summary>
public enum MilvusErrorCode
{
    /// <summary>
    /// The operation succeeded.
    /// </summary>
    Success = 0,

    /// <summary>
    /// An unexpected error occurred.
    /// </summary>
    UnexpectedError = 1,

    /// <summary>
    /// The request was rate-limited by the server.
    /// </summary>
    RateLimit = 8,

    /// <summary>
    /// The request exceeded a service quota.
    /// </summary>
    ServiceQuotaExceeded = 9,

    /// <summary>
    /// The request was rate-limited by a legacy Milvus server.
    /// </summary>
    LegacyRateLimit = 49,

    /// <summary>
    /// The collection schema no longer matches the data sent by the client, usually because the collection was
    /// recreated or altered by another client between operations.
    /// </summary>
    SchemaMismatch = 109,

    /// <summary>
    /// The collection was not found.
    /// </summary>
    CollectionNotFound = 100,

    /// <summary>
    /// The segment was not found.
    /// </summary>
    SegmentNotFound = 600,

    /// <summary>
    /// The index was not found.
    /// </summary>
    IndexNotFound = 700
}
