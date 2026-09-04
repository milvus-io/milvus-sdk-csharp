namespace Milvus.Client.V2.Types;

/// <summary>
/// The load state of a collection or partition.
/// </summary>
public enum LoadState
{
    /// <summary>
    /// The entity does not exist.
    /// </summary>
    NotExist = 0,

    /// <summary>
    /// The entity is not loaded.
    /// </summary>
    NotLoad = 1,

    /// <summary>
    /// The entity is being loaded.
    /// </summary>
    Loading = 2,

    /// <summary>
    /// The entity is loaded.
    /// </summary>
    Loaded = 3
}
