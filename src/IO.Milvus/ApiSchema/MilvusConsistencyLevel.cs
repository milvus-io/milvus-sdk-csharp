namespace IO.Milvus.ApiSchema;

/// <summary>
/// consistency level
/// </summary>
public enum MilvusConsistencyLevel
{
    /// <summary>
    /// Strong
    /// </summary>
    Strong = 0,

    /// <summary>
    /// Session
    /// </summary>
    Session = 1,

    /// <summary>
    /// Bounded
    /// </summary>
    Bounded = 2,

    /// <summary>
    /// Eventually
    /// </summary>
    Eventually = 3,

    /// <summary>
    /// Customized
    /// </summary>
    Customized = 4,
}
