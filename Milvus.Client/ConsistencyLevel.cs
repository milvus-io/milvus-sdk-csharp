namespace Milvus.Client;

/// <summary>
/// The consistency level to be used by a Milvus collection.
/// </summary>
/// <remarks>
/// For more details, see <see href="https://milvus.io/docs/consistency.md" />.
/// </remarks>
public enum ConsistencyLevel
{
    /// <summary>
    /// The highest and the most strict level of consistency. This level ensures that users can read the latest
    /// version of data.
    /// </summary>
    Strong = 0,

    /// <summary>
    /// Ensures that all data writes can be immediately perceived in reads during the same session. In other
    /// words, when you write data via one client, the newly inserted data instantaneously become searchable.
    /// </summary>
    Session = 1,

    /// <summary>
    /// Allows data inconsistency during a certain period of time. However, generally, the data are always globally
    /// consistent out of that period of time.
    /// </summary>
    BoundedStaleness = 2,

    /// <summary>
    /// There is no guaranteed order of reads and writes, and replicas eventually converge to the same state given that
    /// no further write operations are done. Under this level, replicas start working on read requests with the latest
    /// updated values. Eventually consistent is the weakest of the four consistency levels.
    /// </summary>
    Eventually = 3,

    /// <summary>
    /// In this consistency level, users pass their own guarantee timestamp..
    /// </summary>
    Customized = 4
}
