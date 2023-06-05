namespace IO.Milvus;

/// <summary>
/// Show type
/// </summary>
public enum ShowType
{
    /// <summary>
    /// Will return all collections
    /// </summary>
    All = 0,

    /// <summary>
    /// Will return loaded collections with their inMemory_percentages
    /// </summary>
    InMemory = 1,
}
