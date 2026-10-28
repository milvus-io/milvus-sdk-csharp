namespace Milvus.Client.V2.Types;

/// <summary>
/// The consistency level to be used by a Milvus collection.
/// </summary>
/// <remarks>
/// For more details, see <see href="https://milvus.io/docs/consistency.md" />.
/// </remarks>
#pragma warning disable CA1027 // ConsistencyLevel is a sequential value set, not a [Flags] bit mask.
public enum ConsistencyLevel
{
    /// <summary>
    /// The highest and the most strict level of consistency. This level ensures that users can read the latest
    /// version of data.
    /// </summary>
    Strong = 0,

    /// <summary>
    /// Ensures that all data writes can be immediately perceived in reads during the same session. In other words, when
    /// you write data via one client, the newly inserted data instantaneously become searchable.
    /// </summary>
    Session = 1,

    /// <summary>
    /// Allows data inconsistency during a certain period of time. However, generally, the data are always globally
    /// consistent out of that period of time.
    /// </summary>
    Bounded = 2,

    /// <summary>
    /// Allows data inconsistency during a certain period of time. However, generally, the data are always globally
    /// consistent out of that period of time.
    /// </summary>
    /// <remarks>
    /// Deprecated: renamed to <see cref="Bounded" />. This member has the same numeric value (2) and is
    /// kept for compatibility.
    /// </remarks>
#pragma warning disable CA1069 // BoundedStaleness is an intentional deprecated alias of Bounded (same value 2).
    [Obsolete("The BoundedStaleness consistency level is renamed to Bounded to match the Java/C++/Rust/PyMilvus SDKs; use Bounded instead.")]
    BoundedStaleness = 2,
#pragma warning restore CA1069

    /// <summary>
    /// There is no guaranteed order of reads and writes, and replicas eventually converge to the same state given that
    /// no further write operations are done. Under this level, replicas start working on read requests with the latest
    /// updated values. Eventually consistent is the weakest of the four consistency levels.
    /// </summary>
    Eventually = 3,

    /// <summary>
    /// In this consistency level, users pass their own guarantee timestamp.
    /// </summary>
    /// <remarks>
    /// Deprecated: the Java and C++ V2 SDKs no longer expose a Customized level, and the guarantee timestamp is
    /// expected to be derived from the other four levels. This member is kept for compatibility and may be removed
    /// in a future release.
    /// </remarks>
    [Obsolete("The Customized consistency level is deprecated to match the Java/C++ SDKs; prefer Strong, Session, Bounded or Eventually.")]
    Customized = 4
}
#pragma warning restore CA1027
