namespace Milvus.Client.V2.Types;

/// <summary>
/// The quota state of a Milvus server, as reported by <see cref="MilvusClientV2.HealthAsync" />.
/// </summary>
public enum QuotaState
{
    /// <summary>
    /// The quota state is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Reads are rate-limited.
    /// </summary>
    ReadLimited = 2,

    /// <summary>
    /// Writes are rate-limited.
    /// </summary>
    WriteLimited = 3,

    /// <summary>
    /// Reads are denied.
    /// </summary>
    DenyToRead = 4,

    /// <summary>
    /// Writes are denied.
    /// </summary>
    DenyToWrite = 5,

    /// <summary>
    /// DDL operations are denied.
    /// </summary>
    DenyToDdl = 6
}
