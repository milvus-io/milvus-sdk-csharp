namespace Milvus.Client;

/// <summary>
/// Determines which collection to return in an invocation of <see cref="MilvusClient.ListCollectionsAsync" />.
/// </summary>
public enum ListCollectionFilter
{
    /// <summary>
    /// Lists all collections.
    /// </summary>
    All = 0,

    /// <summary>
    /// Lists only connections which have been loaded into memory.
    /// </summary>
    InMemory = 1,
}
