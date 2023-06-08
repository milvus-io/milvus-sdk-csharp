namespace IO.Milvus;

/// <summary>
/// consistency level
/// </summary>
/// <remarks>
/// <see href="https://milvus.io/docs/consistency.md"/>
/// </remarks>
public enum MilvusConsistencyLevel
{
    /// <summary>
    /// Strong
    /// </summary>
    /// <remarks>
    /// Strong is the highest and the most strict level of consistency. It ensures that users can read the latest version of data.
    /// </remarks>
    Strong = 0,

    /// <summary>
    /// Session
    /// </summary>
    /// <remarks>
    /// Session ensures that all data writes can be immediately perceived in reads during the same session. In other words, when you write data via one client, the newly inserted data instantaneously become searchable.
    /// </remarks>
    Session = 1,

    /// <summary>
    /// Bounded
    /// </summary>
    /// <remarks>
    /// Bounded staleness, as its name suggests, allows data inconsistency during a certain period of time. However, generally, the data are always globally consistent out of that period of time.
    /// </remarks>
    Bounded = 2,

    /// <summary>
    /// Eventually
    /// </summary>
    /// <remarks>
    /// There is no guaranteed order of reads and writes, and replicas eventually converge to the same state given that no further write operations are done. Under the consistency of "eventually", replicas start working on read requests with the latest updated values. Eventually consistent is the weakest level among the four.
    /// </remarks>
    Eventually = 3,

    /// <summary>
    /// Customized
    /// </summary>
    Customized = 4,
}
